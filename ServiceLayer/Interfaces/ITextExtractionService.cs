using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface ITextExtractionService
    {
        Task<(bool success, string? text, string? errorMessage)> ExtractTextAsync(string filePath);
    }
}
