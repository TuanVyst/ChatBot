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

        private const string ModelName = "gemini-embedding-2";

        public EmbeddingService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException(
                    nameof(apiKey),
                    "Gemini API key cannot be empty");
            }

            _apiKey = apiKey.Trim();

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            Console.WriteLine(
    $"EmbeddingService key: " +
    $"{_apiKey[..Math.Min(6, _apiKey.Length)]}..." +
    $"{_apiKey[^Math.Min(4, _apiKey.Length)..]}");
        }

        public async Task<(
            bool success,
            List<float>? embedding,
            string? errorMessage)>
        GetEmbeddingAsync(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return (false, null, "Text cannot be empty");
                }

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

                using var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                var requestUrl =
                    $"https://generativelanguage.googleapis.com/v1beta/models/" +
                    $"{ModelName}:embedContent";

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    requestUrl)
                {
                    Content = content
                };

                request.Headers.Add("x-goog-api-key", _apiKey);

                Console.WriteLine("Đang gọi Gemini Embedding API...");

                using var response =
                    await _httpClient.SendAsync(request);

                var responseContent =
                    await response.Content.ReadAsStringAsync();

                Console.WriteLine(
                    $"Embedding response: {(int)response.StatusCode} " +
                    response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine(responseContent);

                    return (
                        false,
                        null,
                        $"Gemini API error: {response.StatusCode} - " +
                        responseContent);
                }

                using var document =
                    JsonDocument.Parse(responseContent);

                var root = document.RootElement;

                if (!root.TryGetProperty(
                        "embedding",
                        out var embeddingObject))
                {
                    return (
                        false,
                        null,
                        "API response không có trường embedding.");
                }

                if (!embeddingObject.TryGetProperty(
                        "values",
                        out var valuesArray) ||
                    valuesArray.ValueKind != JsonValueKind.Array)
                {
                    return (
                        false,
                        null,
                        "API response không có embedding.values.");
                }

                var embedding = new List<float>();

                foreach (var item in valuesArray.EnumerateArray())
                {
                    embedding.Add(item.GetSingle());
                }

                Console.WriteLine(
                    $"Embedding thành công, số chiều: {embedding.Count}");

                return (true, embedding, null);
            }
            catch (TaskCanceledException)
            {
                return (
                    false,
                    null,
                    "Embedding API phản hồi quá lâu và đã bị hủy.");
            }
            catch (HttpRequestException ex)
            {
                return (
                    false,
                    null,
                    $"Không kết nối được Embedding API: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (
                    false,
                    null,
                    $"Embedding failed: {ex.Message}");
            }
        }
    }
}