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

            // Bước 0: Check quota câu hỏi hàng ngày
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var accountId))
            {
                if (!await _subscriptionService.ConsumeQuestionQuotaAsync(accountId))
                {
                    return (false, null, "Bạn đã hết lượt hỏi hôm nay. Hãy đăng ký gói Premium để được hỏi 10 câu/ngày!");
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

            var result = new RagResult
            {
                Answer = answer,

                Sources = chunks
                    .Where(c => c.Document != null)
                    .Select(c => c.Document!.FileName)
                    .Distinct()
                    .ToList(),

                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = totalTokens,
                ModelName = modelName
            };

            // Bước 5: Lưu lịch sử hỏi đáp
            var (saveSuccess, saveError) = await _chatHistoryService.SaveAsync(
                question,
                answer,
                chunks,
                subjectId,
                chapterId,
                userId,
                promptTokens,
                completionTokens,
                totalTokens,
                modelName);

            if (!saveSuccess)
            {
                // Ghi nhận lỗi nhưng không chặn kết quả trả về cho user
                System.Diagnostics.Debug.WriteLine($"Failed to save chat history: {saveError}");
            }

            return (true, result, null);
        }
    }
}
