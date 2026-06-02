using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface IEmbeddingService
    {
        Task<(bool success, List<float>? embedding, string? errorMessage)> GetEmbeddingAsync(string text);
    }
}
