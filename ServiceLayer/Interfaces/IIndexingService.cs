using BusinessObject.Entities;
using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface IIndexingService
    {
        Task<(bool success, string? errorMessage)> IndexDocumentAsync(Document document);
    }
}
