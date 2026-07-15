using BusinessObject.Entities;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Interfaces;

namespace ServiceLayer.Implements;

public class SystemSettingService : ISystemSettingService
{
    private readonly AppDbContext _context;

    public SystemSettingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SystemSetting> GetSettingAsync()
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync();

        if (setting == null)
        {
            setting = new SystemSetting
            {
                Id = 1,
                ChunkSize = 512,
                ChunkOverlap = 50,
                TopK = 5,
                EmbeddingModel = "text-embedding-3-small",
                UpdatedAt = DateTime.UtcNow
            };

            _context.SystemSettings.Add(setting);
            await _context.SaveChangesAsync();
        }

        return setting;
    }

    public async Task<(bool Success, string Message)> UpdateSettingAsync(
        int chunkSize,
        int chunkOverlap,
        int topK,
        string embeddingModel)
    {
        if (chunkSize < 100 || chunkSize > 3000)
            return (false, "Chunk size phải từ 100 đến 3000.");

        if (chunkOverlap < 0 || chunkOverlap >= chunkSize)
            return (false, "Chunk overlap không hợp lệ.");

        if (topK < 1 || topK > 20)
            return (false, "Top K phải từ 1 đến 20.");

        var setting = await GetSettingAsync();

        setting.ChunkSize = chunkSize;
        setting.ChunkOverlap = chunkOverlap;
        setting.TopK = topK;
        setting.EmbeddingModel = embeddingModel;
        setting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Cập nhật cấu hình thành công.");
    }
}