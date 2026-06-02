using BusinessObject.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface IDocumentChunkService
    {
        Task<IEnumerable<DocumentChunk>> GetDocumentChunksByDocumentIdAsync(int documentId);
    }
}
