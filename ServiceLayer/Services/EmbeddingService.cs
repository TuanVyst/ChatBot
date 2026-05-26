using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace ServiceLayer.Services
{
    public class EmbeddingService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        public EmbeddingService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey), "OpenAI API key cannot be empty");
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }
        public async Task<(bool success, List<float>? embedding, string? errorMessage)> GetEmbeddingAsync(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return (false, null, "Text cannot be empty");
                var requestBody = new
                {
                    model = "text-embedding-ada-002",
                    input = text
                };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings")
                {
                    Content = content
                };
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    return (false, null, $"OpenAI API error: {response.StatusCode} - {responseContent}");
                using (var doc = JsonDocument.Parse(responseContent))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                    {
                        var firstItem = dataArray[0];
                        if (firstItem.TryGetProperty("embedding", out var embeddingArray))
                        {
                            var embedding = new List<float>();
                            foreach (var item in embeddingArray.EnumerateArray())
                            {
                                embedding.Add(item.GetSingle());
                            }
                            return (true, embedding, null);
                        }
                    }
                }
                return (false, null, "No embedding in API response");
            }
            catch (Exception ex)
            {
                return (false, null, $"Embedding failed: {ex.Message}");
            }
        }
    }
}
