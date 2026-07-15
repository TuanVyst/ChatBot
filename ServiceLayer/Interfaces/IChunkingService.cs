using System.Collections.Generic;

namespace ServiceLayer.Interfaces
{
    public interface IChunkingService
    {
        List<string> ChunkText(string text, int chunkSize, int overlapSize);
    }
}