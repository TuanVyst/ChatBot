using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using ServiceLayer.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class DocumentChunkService : IDocumentChunkService
    {
        private readonly IDocumentChunkRepository _documentChunkRepository;

        public DocumentChunkService(IDocumentChunkRepository documentChunkRepository)
        {
            _documentChunkRepository = documentChunkRepository;
        }

        public async Task<IEnumerable<DocumentChunk>> GetDocumentChunksByDocumentIdAsync(int documentId)
        {
            return await _documentChunkRepository.GetByDocumentIdAsync(documentId);
        }
    }
}
