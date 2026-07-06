using BusinessObject.Entities;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class ChatHistoryService : IChatHistoryService
    {
        private readonly AppDbContext _context;

        public ChatHistoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool success, string? errorMessage)> SaveAsync(
            string question, string answer, List<DocumentChunk> retrievedChunks,
            Guid? subjectId, Guid? chapterId, string? userId)
        {
            try
            {
                var chatHistory = new ChatHistory
                {
                    Question = question,
                    Answer = answer,
                    SubjectId = subjectId,
                    ChapterId = chapterId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Sources = retrievedChunks.Select(chunk => new ChatHistorySource
                    {
                        DocumentChunkId = chunk.Id
                    }).ToList()
                };

                _context.ChatHistories.Add(chatHistory);
                await _context.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Save chat history failed: {ex.Message}");
            }
        }

        public async Task<(bool success, List<ChatHistory>? history, string? errorMessage)> GetHistoryAsync(
            string? userId, Guid? subjectId = null, Guid? chapterId = null, int take = 20)
        {
            try
            {
                var query = _context.ChatHistories
                    .Include(ch => ch.Sources)
                        .ThenInclude(s => s.DocumentChunk)
                            .ThenInclude(dc => dc!.Document)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                    query = query.Where(ch => ch.UserId == userId);

                if (subjectId.HasValue)
                    query = query.Where(ch => ch.SubjectId == subjectId.Value);

                if (chapterId.HasValue)
                    query = query.Where(ch => ch.ChapterId == chapterId.Value);

                var results = await query
                    .OrderByDescending(ch => ch.CreatedAt)
                    .Take(take)
                    .ToListAsync();

                return (true, results, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Get chat history failed: {ex.Message}");
            }
        }
    }
}
