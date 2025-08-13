using System.Text;
using System.Text.Json;
using RestSharp;

namespace TelegramGPTBot.Providers;

public class ChatGPTPlusProvider : IChatProvider
{
    private readonly ChatGPTPlusConfig _config;
    private readonly RestClient _client;
    private string? _sessionToken;
    private DateTime _lastTokenRefresh = DateTime.MinValue;

    public ChatGPTPlusProvider(ChatGPTPlusConfig config)
    {
        _config = config;
        _client = new RestClient("https://chat.openai.com");
    }

    public async Task<string> GetResponseAsync(string userInput, CancellationToken cancellationToken = default)
    {
        try
        {
            // Обновляем токен сессии если нужно
            if (string.IsNullOrEmpty(_sessionToken) || DateTime.Now - _lastTokenRefresh > TimeSpan.FromHours(1))
            {
                await RefreshSessionTokenAsync();
            }

            if (string.IsNullOrEmpty(_sessionToken))
            {
                return "Ошибка: Не удалось получить токен сессии ChatGPT+";
            }

            // Отправляем запрос к ChatGPT+
            var response = await SendChatRequestAsync(userInput, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            return $"Ошибка ChatGPT+: {ex.Message}";
        }
    }

    private async Task RefreshSessionTokenAsync()
    {
        try
        {
            // Логинимся в ChatGPT+
            var loginRequest = new RestRequest("/api/auth/session", Method.Post);
            loginRequest.AddHeader("Content-Type", "application/json");
            
            var loginBody = new
            {
                email = _config.Email,
                password = _config.Password
            };
            
            loginRequest.AddJsonBody(loginBody);
            
            var loginResponse = await _client.ExecuteAsync(loginRequest);
            
            if (loginResponse.IsSuccessful && !string.IsNullOrEmpty(loginResponse.Content))
            {
                // Извлекаем токен из ответа
                var json = JsonDocument.Parse(loginResponse.Content);
                if (json.RootElement.TryGetProperty("accessToken", out var tokenElement))
                {
                    _sessionToken = tokenElement.GetString();
                    _lastTokenRefresh = DateTime.Now;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обновлении токена ChatGPT+: {ex.Message}");
        }
    }

    private async Task<string> SendChatRequestAsync(string userInput, CancellationToken cancellationToken)
    {
        var chatRequest = new RestRequest("/backend-api/conversation", Method.Post);
        chatRequest.AddHeader("Authorization", $"Bearer {_sessionToken}");
        chatRequest.AddHeader("Content-Type", "application/json");
        chatRequest.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        
        var chatBody = new
        {
            action = "next",
            messages = new[]
            {
                new
                {
                    id = Guid.NewGuid().ToString(),
                    role = "user",
                    content = new
                    {
                        content_type = "text",
                        parts = new[] { userInput }
                    }
                }
            },
            model = "gpt-4",
            parent_message_id = Guid.NewGuid().ToString()
        };
        
        chatRequest.AddJsonBody(chatBody);
        
        var response = await _client.ExecuteAsync(chatRequest, cancellationToken);
        
        if (!response.IsSuccessful)
        {
            return $"Ошибка ChatGPT+ API: {response.StatusCode} - {response.StatusDescription}";
        }

        try
        {
            // Парсим ответ ChatGPT+
            var json = JsonDocument.Parse(response.Content!);
            if (json.RootElement.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement) &&
                contentElement.TryGetProperty("parts", out var partsElement))
            {
                var parts = partsElement.EnumerateArray();
                var responseText = new StringBuilder();
                
                foreach (var part in parts)
                {
                    if (part.TryGetProperty("text", out var textElement))
                    {
                        responseText.Append(textElement.GetString());
                    }
                }
                
                return responseText.ToString().Trim();
            }
            
            return "Пустой ответ от ChatGPT+";
        }
        catch (Exception ex)
        {
            return $"Ошибка при обработке ответа ChatGPT+: {ex.Message}";
        }
    }

    public string GetProviderName() => "ChatGPT+ Web";

    public bool IsAvailable() => !string.IsNullOrEmpty(_config.Email) && !string.IsNullOrEmpty(_config.Password);
}
