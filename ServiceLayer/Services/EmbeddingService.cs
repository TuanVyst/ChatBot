using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAI;
namespace ServiceLayer.Services
{
    public class EmbeddingService
    {
        private readonly string _apiKey;
        public EmbeddingService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey), "OpenAI API key cannot be empty");
            _apiKey = apiKey;
        }
        public async Task<(bool success, List<float>? embedding, string? errorMessage)> GetEmbeddingAsync(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return (false, null, "Text cannot be empty");
                var client = new OpenAIClient(_apiKey);
                var embeddingRequest = new OpenAI.Embeddings.EmbeddingRequest 
                { 
                    Model = "text-embedding-ada-002", 
                    Input = text 
                };
                var embeddings = await client.Embeddings.CreateEmbeddingsAsync(embeddingRequest);
                if (embeddings?.Data == null || embeddings.Data.Count == 0)
                    return (false, null, "No embedding returned from API");
                var embedding = new List<float>(embeddings.Data[0].Embedding);
                return (true, embedding, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Embedding failed: {ex.Message}");
            }
        }
    }
}
