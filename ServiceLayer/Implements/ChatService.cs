using ServiceLayer.Interfaces;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class ChatService : IChatService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        private const string ModelName = "gemini-3.5-flash";

        public ChatService(string apiKey)
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
                Timeout = TimeSpan.FromSeconds(60)
            };

            Console.WriteLine(
    $"ChatService key: " +
    $"{_apiKey[..Math.Min(6, _apiKey.Length)]}..." +
    $"{_apiKey[^Math.Min(4, _apiKey.Length)..]}");
        }

        public async Task<(
            bool success,
            string? answer,
            int promptTokens,
            int completionTokens,
            int totalTokens,
            string modelName,
            string? errorMessage)>
        GenerateAnswerAsync(string prompt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    return (
                        false,
                        null,
                        0,
                        0,
                        0,
                        ModelName,
                        "Prompt cannot be empty");
                }

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = prompt
                                }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);

                var requestUrl =
                    $"https://generativelanguage.googleapis.com/v1beta/models/" +
                    $"{ModelName}:generateContent";

                int maxRetries = 3;
                HttpResponseMessage? response = null;
                string responseContent = string.Empty;

                for (int i = 0; i < maxRetries; i++)
                {
                    using var content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                    using var request = new HttpRequestMessage(
                        HttpMethod.Post,
                        requestUrl)
                    {
                        Content = content
                    };

                    request.Headers.Add("x-goog-api-key", _apiKey);

                    Console.WriteLine($"Đang gửi request đến Gemini... (Lần {i + 1})");

                    response = await _httpClient.SendAsync(request);
                    responseContent = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"Gemini response: {(int)response.StatusCode} {response.StatusCode}");

                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || 
                        response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || 
                        response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        if (i < maxRetries - 1)
                        {
                            var delay = (int)Math.Pow(2, i) * 1000; // 1s, 2s
                            Console.WriteLine($"API quá tải. Thử lại sau {delay}ms...");
                            await Task.Delay(delay);
                            continue;
                        }
                    }
                    
                    break; // Success or non-retriable error
                }

                Console.WriteLine(responseContent);

                if (response == null || !response.IsSuccessStatusCode)
                {
                    return (
                        false,
                        null,
                        0,
                        0,
                        0,
                        ModelName,
                        $"Gemini API error: {response.StatusCode} - {responseContent}");
                }

                using var document =
                    JsonDocument.Parse(responseContent);

                var root = document.RootElement;

                string? answer = null;

                if (root.TryGetProperty(
                        "candidates",
                        out var candidates) &&
                    candidates.ValueKind == JsonValueKind.Array &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];

                    if (firstCandidate.TryGetProperty(
                            "content",
                            out var contentObject) &&
                        contentObject.TryGetProperty(
                            "parts",
                            out var parts) &&
                        parts.ValueKind == JsonValueKind.Array &&
                        parts.GetArrayLength() > 0)
                    {
                        var answerBuilder = new StringBuilder();

                        foreach (var part in parts.EnumerateArray())
                        {
                            if (part.TryGetProperty(
                                    "text",
                                    out var textElement))
                            {
                                var text = textElement.GetString();

                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    answerBuilder.Append(text);
                                }
                            }
                        }

                        answer = answerBuilder.ToString();
                    }
                }

                if (string.IsNullOrWhiteSpace(answer))
                {
                    return (
                        false,
                        null,
                        0,
                        0,
                        0,
                        ModelName,
                        "No answer in API response");
                }

                var promptTokens = 0;
                var completionTokens = 0;
                var totalTokens = 0;

                if (root.TryGetProperty(
                        "usageMetadata",
                        out var usageMetadata))
                {
                    if (usageMetadata.TryGetProperty(
                            "promptTokenCount",
                            out var promptTokenElement) &&
                        promptTokenElement.ValueKind ==
                            JsonValueKind.Number)
                    {
                        promptTokens =
                            promptTokenElement.GetInt32();
                    }

                    if (usageMetadata.TryGetProperty(
                            "candidatesTokenCount",
                            out var completionTokenElement) &&
                        completionTokenElement.ValueKind ==
                            JsonValueKind.Number)
                    {
                        completionTokens =
                            completionTokenElement.GetInt32();
                    }

                    if (usageMetadata.TryGetProperty(
                            "totalTokenCount",
                            out var totalTokenElement) &&
                        totalTokenElement.ValueKind ==
                            JsonValueKind.Number)
                    {
                        totalTokens =
                            totalTokenElement.GetInt32();
                    }
                }

                if (totalTokens == 0)
                {
                    totalTokens =
                        promptTokens + completionTokens;
                }

                return (
                    true,
                    answer,
                    promptTokens,
                    completionTokens,
                    totalTokens,
                    ModelName,
                    null);
            }
            catch (TaskCanceledException)
            {
                return (
                    false,
                    null,
                    0,
                    0,
                    0,
                    ModelName,
                    "Gemini phản hồi quá lâu. Request đã bị hủy sau 30 giây.");
            }
            catch (HttpRequestException ex)
            {
                return (
                    false,
                    null,
                    0,
                    0,
                    0,
                    ModelName,
                    $"Không thể kết nối Gemini API: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (
                    false,
                    null,
                    0,
                    0,
                    0,
                    ModelName,
                    $"Chat generation failed: {ex.Message}");
            }
        }
    }
}