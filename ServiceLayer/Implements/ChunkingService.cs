using ServiceLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLayer.Implements
{
    public class ChunkingService : IChunkingService
    {
        public List<string> ChunkText(string text, int chunkSize, int overlapSize)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var chunks = new List<string>();
            var lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var currentChunk = new StringBuilder();

            foreach (var line in lines)
            {
                if (currentChunk.Length + line.Length + 1 > chunkSize)
                {
                    if (currentChunk.Length > 0)
                    {
                        chunks.Add(currentChunk.ToString().Trim());

                        var overlapStart = Math.Max(0, currentChunk.Length - overlapSize);
                        currentChunk = new StringBuilder(currentChunk.ToString().Substring(overlapStart));
                    }
                }

                if (currentChunk.Length > 0)
                    currentChunk.Append("\n");

                currentChunk.Append(line);
            }

            if (currentChunk.Length > 0)
                chunks.Add(currentChunk.ToString().Trim());

            return chunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        }
    }
}