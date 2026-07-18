using BusinessObject.Entities;
using ServiceLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class RagService : IRagService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IRetrievalService _retrievalService;
        private readonly IChatService _chatService;
        private readonly IChatHistoryService _chatHistoryService;
        private readonly ISubscriptionService _subscriptionService;

        public RagService(
            IEmbeddingService embeddingService,
            IRetrievalService retrievalService,
            IChatService chatService,
            IChatHistoryService chatHistoryService,
            ISubscriptionService subscriptionService)
        {
            _embeddingService = embeddingService;
            _retrievalService = retrievalService;
            _chatService = chatService;
            _chatHistoryService = chatHistoryService;
            _subscriptionService = subscriptionService;
        }

        public async Task<(bool success, RagResult? result, string? errorMessage)> AskAsync(
            string question, Guid? subjectId = null, Guid? chapterId = null, int? documentId = null, string? userId = null)
        {
            if (string.IsNullOrWhiteSpace(question))
                return (false, null, "Question cannot be empty");

            // Bước 0: Check quota token hàng ngày
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var accountId))
            {
                if (!await _subscriptionService.HasRemainingTokenQuotaAsync(accountId))
                {
                    return (false, null, "Bạn đã dùng hết hạn mức token hôm nay. Vui lòng đăng ký/nâng cấp Premium để tiếp tục!");
                }
            }

            // Bước 1: Embed câu hỏi
            var (embedSuccess, embedding, embedError) = await _embeddingService.GetEmbeddingAsync(question);
            if (!embedSuccess || embedding == null)
                return (false, null, $"Embedding failed: {embedError}");

            // Bước 2: Retrieve top-k chunk liên quan, có filter theo Subject/Chapter/Document
            var (searchSuccess, chunks, searchError) = await _retrievalService.SearchAsync(
                embedding, topK: 5, subjectId, chapterId, documentId);

            if (!searchSuccess || chunks == null || chunks.Count == 0)
                return (false, null, $"No relevant context found. {searchError}");

            // Bước 3: Build prompt từ context
            var contextBuilder = new StringBuilder();
            foreach (var chunk in chunks)
            {
                var fileName = chunk.Document?.FileName ?? "Không rõ nguồn";
                contextBuilder.AppendLine($"[Nguồn: {fileName}]");
                contextBuilder.AppendLine(chunk.Content);
                contextBuilder.AppendLine("---");
            }

            var prompt = $"""
                Bạn là trợ lý trả lời câu hỏi dựa trên tài liệu được cung cấp.
                Chỉ trả lời dựa trên ngữ cảnh dưới đây. Nếu ngữ cảnh không chứa thông tin 
                liên quan, hãy nói rõ "Tôi không tìm thấy thông tin này trong tài liệu" — 
                không được tự bịa ra câu trả lời.

                Bắt buộc ở cuối câu trả lời của bạn, phải có một dòng duy nhất theo định dạng chính xác sau để chỉ định nguồn tài liệu:
                [SOURCES]: tên_file_1, tên_file_2
                Trong đó, tên_file_1, tên_file_2 là danh sách tên các file nguồn (ví dụ: tailieu.pdf) mà bạn thực sự sử dụng thông tin từ đó để tạo câu trả lời. Nếu câu trả lời không sử dụng nguồn nào hoặc bạn tự trả lời, hãy ghi: [SOURCES]: None.

                Ngữ cảnh:
                {contextBuilder}

                Câu hỏi: {question}
                """;

            // Bước 4: Gọi Gemini sinh câu trả lời
            var (
               chatSuccess,
               answer,
               promptTokens,
               completionTokens,
               totalTokens,
               modelName,
               chatError
            ) = await _chatService.GenerateAnswerAsync(prompt);
            if (!chatSuccess || answer == null)
                return (false, null, $"Chat generation failed: {chatError}");

            // Phân tích và làm sạch câu trả lời, tách biệt phần SOURCES thô
            string cleanAnswer = answer;
            var sources = new List<string>();
            var marker = "[SOURCES]:";
            int markerIndex = answer.LastIndexOf(marker);

            if (markerIndex == -1)
            {
                marker = "SOURCES:";
                markerIndex = answer.LastIndexOf(marker);
            }

            if (markerIndex != -1)
            {
                var sourcesLine = answer.Substring(markerIndex);
                cleanAnswer = answer.Substring(0, markerIndex).Trim();

                var filesPart = sourcesLine.Replace(marker, "").Replace("**", "").Trim();
                if (!string.Equals(filesPart, "None", StringComparison.OrdinalIgnoreCase))
                {
                    var fileNames = filesPart.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(f => f.Trim().Trim('[', ']', '`', '"', '*'))
                                             .ToList();

                    var retrievedFiles = chunks
                        .Where(c => c.Document != null)
                        .Select(c => c.Document!.FileName)
                        .Distinct()
                        .ToList();

                    foreach (var file in fileNames)
                    {
                        var matchedFile = retrievedFiles.FirstOrDefault(rf =>
                            string.Equals(rf, file, StringComparison.OrdinalIgnoreCase) ||
                            rf.Contains(file, StringComparison.OrdinalIgnoreCase) ||
                            file.Contains(rf, StringComparison.OrdinalIgnoreCase));

                        if (matchedFile != null && !sources.Contains(matchedFile))
                        {
                            sources.Add(matchedFile);
                        }
                    }
                }
            }

            // Fallback nếu không parse được nguồn nào
            if (sources.Count == 0 && chunks.Any())
            {
                sources = chunks
                    .Where(c => c.Document != null)
                    .Select(c => c.Document!.FileName)
                    .Distinct()
                    .ToList();
            }

            // Lọc danh sách chunks thực sự được sử dụng để lưu trữ lịch sử chính xác
            var filteredChunks = chunks
                .Where(c => c.Document != null && sources.Contains(c.Document.FileName))
                .ToList();

            if (filteredChunks.Count == 0)
            {
                filteredChunks = chunks;
            }

            var result = new RagResult
            {
                Answer = cleanAnswer,
                Sources = sources,
                RetrievedChunks = filteredChunks,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = totalTokens,
                ModelName = modelName
            };

            // Bước 5: Lưu lịch sử hỏi đáp với câu trả lời sạch và chunks đã lọc
            var (saveSuccess, saveError) = await _chatHistoryService.SaveAsync(
                question,
                cleanAnswer,
                filteredChunks,
                subjectId,
                chapterId,
                userId,
                promptTokens,
                completionTokens,
                totalTokens,
                modelName);

            if (!saveSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save chat history: {saveError}");
            }

            return (true, result, null);
        }
    }
}
