using ServiceLayer.Interfaces;
using System;
using System.Collections.Generic;

namespace ServiceLayer.Implements
{
    public class ChunkingService : IChunkingService
    {
        public List<string> ChunkText(string text, int chunkSize, int overlapSize)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            // ── Step 1: Split into whitespace-delimited tokens ──
            var tokens = text.Split(new[] { ' ', '\r', '\n', '\t' },
                                    StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
                return new List<string>();

            // ── Step 2: Giống MS Word: mọi token đều là 1 từ ──
            // Word tính tất cả token phân cách bởi space là 1 từ:
            // "HTTP/2" → 1 từ, "don't" → 1 từ, "-" → 1 từ, "--" → 1 từ.
            int totalWords = tokens.Length;

            // Vì mỗi token = 1 từ, wordIndex == tokenIndex. Không cần prefix-sum.

            // ── Step 4: Slice into chunks ──
            int wordStep = chunkSize - overlapSize;
            if (wordStep <= 0)
                wordStep = Math.Max(1, chunkSize / 2);

            var chunks = new List<string>();

            for (int chunkStart = 0; chunkStart < totalWords; chunkStart += wordStep)
            {
                // Vì wordIndex == tokenIndex, mapping trực tiếp
                int startTok = chunkStart;
                int endTok   = Math.Min(chunkStart + chunkSize - 1, totalWords - 1);

                int count = endTok - startTok + 1;
                var chunkText = string.Join(" ", tokens, startTok, count);
                chunks.Add(chunkText);

                // Stop nếu đã lấy đến token cuối
                if (endTok >= totalWords - 1)
                    break;
            }

            return chunks;
        }

    }
}