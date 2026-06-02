using ServiceLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        
        // Sử dụng model embedding mới nhất của Gemini
        private const string ModelName = "gemini-embedding-001"; 

        public EmbeddingService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey), "Gemini API key cannot be empty");
            
            _apiKey = apiKey.Trim();
            _httpClient = new HttpClient();
        }

        public async Task<(bool success, List<float>? embedding, string? errorMessage)> GetEmbeddingAsync(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return (false, null, "Text cannot be empty");

            

                // 1. Tạo request body theo chuẩn của Gemini API
                var requestBody = new
                {
                    model = $"models/{ModelName}",
                    content = new
                    {
                        parts = new[]
                        {
                            new { text }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // THAY BẰNG DÒNG NÀY:
                string requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:embedContent?key={_apiKey}";

                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);

          
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, null, $"Gemini API error: {response.StatusCode} - {responseContent}");

                // 3. Parse JSON trả về theo cấu trúc của Gemini
                using (var doc = JsonDocument.Parse(responseContent))
                {
                    var root = doc.RootElement;
                    
                    // Gemini trả về object "embedding" chứa mảng "values"
                    if (root.TryGetProperty("embedding", out var embeddingObject))
                    {
                        if (embeddingObject.TryGetProperty("values", out var valuesArray))
                        {
                            var embedding = new List<float>();
                            foreach (var item in valuesArray.EnumerateArray())
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