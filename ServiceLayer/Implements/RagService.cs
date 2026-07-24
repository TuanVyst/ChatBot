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
                embedding, topK: 5, subjectId, chapterId, documentId, maxDistance: 0.75);

            if (!searchSuccess || chunks == null || chunks.Count == 0)
                return (false, null, $"No relevant context found. {searchError}");

            // Bước 3: Build prompt từ context
            var contextBuilder = new StringBuilder();
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var fileName = chunk.Document?.FileName ?? "Không rõ nguồn";
                contextBuilder.AppendLine($"[Nguồn {i + 1}: {fileName} (Đoạn {chunk.ChunkOrder})]");
                contextBuilder.AppendLine(chunk.Content);
                contextBuilder.AppendLine("---");
            }

            var prompt = $"""
                Bạn là trợ lý học tập trả lời câu hỏi dựa trên tài liệu được cung cấp.
                Chỉ trả lời dựa trên ngữ cảnh dưới đây. Nếu ngữ cảnh không chứa thông tin 
                liên quan, hãy nói rõ "Tôi không tìm thấy thông tin này trong tài liệu" — 
                không được tự bịa ra câu trả lời.

                QUY TẮC TRÍCH DẪN RẤT QUAN TRỌNG:
                1. Khi đưa ra bất kỳ ý kiến, khẳng định hoặc thông tin nào lấy từ ngữ cảnh, bạn BẮT BUỘC chèn nhãn trích dẫn số vuông như [1], [2], [3]... (tương ứng với [Nguồn 1], [Nguồn 2]...) ngay sau câu hoặc ý đó.
                2. Một câu có thể chứa nhiều trích dẫn (ví dụ: [1][2]).
                3. Ở cuối cùng của câu trả lời, hãy đính kèm duy nhất 1 dòng chỉ định nguồn bạn sử dụng:
                [SOURCES]: [1], [2] (hoặc [SOURCES]: None nếu không dùng).

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
            var filteredChunks = new List<DocumentChunk>();
            var marker = "[SOURCES]:";
            int markerIndex = answer.LastIndexOf(marker);

            if (markerIndex == -1)
            {
                marker = "SOURCES:";
                markerIndex = answer.LastIndexOf(marker);
            }

            if (markerIndex != -1)
            {
                cleanAnswer = answer.Substring(0, markerIndex).Trim();
            }

            // Ưu tiên 1: Tách các chỉ số trích dẫn [1], [2] xuất hiện trong câu trả lời hoặc dòng SOURCES
            var citedIndices = new List<int>();
            var numberMatches = System.Text.RegularExpressions.Regex.Matches(answer, @"\[(?<num>\d+)\]");
            foreach (System.Text.RegularExpressions.Match m in numberMatches)
            {
                if (int.TryParse(m.Groups["num"].Value, out int idx) && idx >= 1 && idx <= chunks.Count)
                {
                    if (!citedIndices.Contains(idx))
                    {
                        citedIndices.Add(idx);
                    }
                }
            }

            if (citedIndices.Any())
            {
                // Lấy chính xác các chunks tương ứng với chỉ số [1], [2]... (với idx - 1)
                foreach (var idx in citedIndices)
                {
                    var c = chunks[idx - 1];
                    if (!filteredChunks.Contains(c))
                    {
                        filteredChunks.Add(c);
                    }
                    if (c.Document != null && !sources.Contains(c.Document.FileName))
                    {
                        sources.Add(c.Document.FileName);
                    }
                }
            }
            else if (markerIndex != -1)
            {
                // Fallback 1: Parse theo định dạng tên_file (Đoạn X)
                var sourcesLine = answer.Substring(markerIndex);
                var filesPart = sourcesLine.Replace(marker, "").Replace("**", "").Trim();
                if (!string.Equals(filesPart, "None", StringComparison.OrdinalIgnoreCase))
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(
                        filesPart,
                        @"(?<file>[^\,\;\(\)]+?)(?:\s*\([Đđ]oạn\s*(?<order>\d+)\))?(?:[\,\;\)]|$)");

                    var retrievedFiles = chunks
                        .Where(c => c.Document != null)
                        .Select(c => c.Document!.FileName)
                        .Distinct()
                        .ToList();

                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        var rawFile = match.Groups["file"].Value.Trim().Trim('[', ']', '`', '"', '*');
                        if (string.IsNullOrWhiteSpace(rawFile) || string.Equals(rawFile, "None", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var matchedFile = retrievedFiles.FirstOrDefault(rf =>
                            string.Equals(rf, rawFile, StringComparison.OrdinalIgnoreCase) ||
                            rf.Contains(rawFile, StringComparison.OrdinalIgnoreCase) ||
                            rawFile.Contains(rf, StringComparison.OrdinalIgnoreCase));

                        if (matchedFile != null)
                        {
                            if (!sources.Contains(matchedFile))
                            {
                                sources.Add(matchedFile);
                            }

                            int? chunkOrder = null;
                            if (match.Groups["order"].Success && int.TryParse(match.Groups["order"].Value, out int orderVal))
                            {
                                chunkOrder = orderVal;
                            }

                            var fileChunks = chunks.Where(c => c.Document != null && string.Equals(c.Document.FileName, matchedFile, StringComparison.OrdinalIgnoreCase));

                            if (chunkOrder.HasValue)
                            {
                                var targetChunk = fileChunks.FirstOrDefault(c => c.ChunkOrder == chunkOrder.Value);
                                if (targetChunk != null && !filteredChunks.Contains(targetChunk))
                                {
                                    filteredChunks.Add(targetChunk);
                                }
                            }
                            else
                            {
                                foreach (var fc in fileChunks)
                                {
                                    if (!filteredChunks.Contains(fc))
                                    {
                                        filteredChunks.Add(fc);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Fallback 2: Nếu hoàn toàn không lọc được trích dẫn cụ thể nào, mới giữ danh sách gốc
            if (filteredChunks.Count == 0)
            {
                filteredChunks = chunks;
                sources = chunks
                    .Where(c => c.Document != null)
                    .Select(c => c.Document!.FileName)
                    .Distinct()
                    .ToList();
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
