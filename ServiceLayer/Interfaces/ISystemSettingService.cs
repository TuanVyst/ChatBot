using BusinessObject.Entities;

namespace ServiceLayer.Interfaces;

public interface ISystemSettingService
{
    Task<SystemSetting> GetSettingAsync();

    Task<(bool Success, string Message)> UpdateSettingAsync(
        int chunkSize,
        int chunkOverlap,
        int topK,
        string embeddingModel,
        string backupEmbeddingModel);
}