using ServiceLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class ChatService : IChatService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        
        private const string ModelName = "gemini-2.5-flash";

        public ChatService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey), "Gemini API key cannot be empty");

            _apiKey = apiKey.Trim();
            _httpClient = new HttpClient();
        }

        public async Task<(bool success, string? answer, string? errorMessage)> GenerateAnswerAsync(string prompt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return (false, null, "Prompt cannot be empty");

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent?key={_apiKey}";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, null, $"Gemini API error: {response.StatusCode} - {responseContent}");

                using (var doc = JsonDocument.Parse(responseContent))
                {
                    var root = doc.RootElement;

                    // Gemini trả về: candidates[0].content.parts[0].text
                    if (root.TryGetProperty("candidates", out var candidates) &&
                        candidates.GetArrayLength() > 0)
                    {
                        var firstCandidate = candidates[0];
                        if (firstCandidate.TryGetProperty("content", out var contentObj) &&
                            contentObj.TryGetProperty("parts", out var parts) &&
                            parts.GetArrayLength() > 0)
                        {
                            var text = parts[0].GetProperty("text").GetString();
                            return (true, text, null);
                        }
                    }
                }

                return (false, null, "No answer in API response");
            }
            catch (Exception ex)
            {
                return (false, null, $"Chat generation failed: {ex.Message}");
            }
        }
    }

}
