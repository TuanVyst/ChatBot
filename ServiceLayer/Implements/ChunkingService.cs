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
                if (line.Length > chunkSize)
                {
                    // Nếu dòng quá dài, đóng chunk hiện tại lại trước
                    if (currentChunk.Length > 0)
                    {
                        chunks.Add(currentChunk.ToString().Trim());
                        currentChunk.Clear();
                    }

                    // Phân rã dòng siêu dài thành các đoạn nhỏ hơn giới hạn
                    var lineSegments = SplitLongLine(line, chunkSize, overlapSize);
                    for (int i = 0; i < lineSegments.Count; i++)
                    {
                        if (i < lineSegments.Count - 1)
                        {
                            // Đưa các đoạn đầy đủ trực tiếp vào danh sách chunk
                            chunks.Add(lineSegments[i]);
                        }
                        else
                        {
                            // Đoạn cuối cùng giữ lại trong currentChunk để có thể gộp với các dòng tiếp theo
                            currentChunk.Append(lineSegments[i]);
                        }
                    }
                }
                else
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
            }

            if (currentChunk.Length > 0)
                chunks.Add(currentChunk.ToString().Trim());

            return chunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        }

        private List<string> SplitLongLine(string line, int chunkSize, int overlapSize)
        {
            var segments = new List<string>();
            if (string.IsNullOrEmpty(line)) return segments;
            
            if (line.Length <= chunkSize)
            {
                segments.Add(line);
                return segments;
            }

            int index = 0;
            while (index < line.Length)
            {
                int lengthToTake = Math.Min(chunkSize, line.Length - index);
                
                // Cố gắng cắt tại vị trí dấu cách để tránh đứt từ
                if (index + lengthToTake < line.Length)
                {
                    int lastSpaceIndex = line.LastIndexOf(' ', index + lengthToTake, lengthToTake);
                    if (lastSpaceIndex > index && lastSpaceIndex - index >= chunkSize / 2)
                    {
                        lengthToTake = lastSpaceIndex - index;
                    }
                }

                var segment = line.Substring(index, lengthToTake).Trim();
                if (!string.IsNullOrEmpty(segment))
                {
                    segments.Add(segment);
                }

                int prevIndex = index;
                index += lengthToTake;
                if (index < line.Length)
                {
                    index = Math.Max(index - overlapSize, 0);
                    
                    // Đảm bảo chỉ mục luôn tiến lên để tránh vòng lặp vô tận
                    if (index <= prevIndex)
                    {
                        index = prevIndex + 1;
                    }
                }
            }

            return segments;
        }
    }
}