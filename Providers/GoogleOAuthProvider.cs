using System.Text;
using System.Text.Json;
using RestSharp;

namespace TelegramGPTBot.Providers;

public class GoogleOAuthProvider : IChatProvider
{
    private readonly GoogleOAuthConfig _config;
    private readonly RestClient _googleClient;
    private readonly RestClient _openaiClient;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public GoogleOAuthProvider(GoogleOAuthConfig config)
    {
        _config = config;
        _googleClient = new RestClient("https://oauth2.googleapis.com");
        _openaiClient = new RestClient("https://chat.openai.com");
    }

    public async Task<string> GetResponseAsync(string userInput, CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяем и обновляем токен если нужно
            if (string.IsNullOrEmpty(_accessToken) || DateTime.Now >= _tokenExpiry)
            {
                await RefreshAccessTokenAsync();
            }

            if (string.IsNullOrEmpty(_accessToken))
            {
                return "Ошибка: Не удалось получить токен доступа Google OAuth";
            }

            // Используем токен для доступа к ChatGPT+
            var response = await SendChatRequestAsync(userInput, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            return $"Ошибка Google OAuth: {ex.Message}";
        }
    }

    private async Task RefreshAccessTokenAsync()
    {
        try
        {
            // Получаем новый access token через refresh token
            var tokenRequest = new RestRequest("/token", Method.Post);
            tokenRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            var tokenBody = new
            {
                client_id = _config.ClientId,
                client_secret = _config.ClientSecret,
                refresh_token = _config.RefreshToken,
                grant_type = "refresh_token"
            };

            tokenRequest.AddParameter("client_id", _config.ClientId);
            tokenRequest.AddParameter("client_secret", _config.ClientSecret);
            tokenRequest.AddParameter("refresh_token", _config.RefreshToken);
            tokenRequest.AddParameter("grant_type", "refresh_token");

            var tokenResponse = await _googleClient.ExecuteAsync(tokenRequest);

            if (tokenResponse.IsSuccessful && !string.IsNullOrEmpty(tokenResponse.Content))
            {
                var json = JsonDocument.Parse(tokenResponse.Content);
                if (json.RootElement.TryGetProperty("access_token", out var tokenElement))
                {
                    _accessToken = tokenElement.GetString();
                    
                    // Устанавливаем время истечения токена (обычно 1 час)
                    if (json.RootElement.TryGetProperty("expires_in", out var expiresElement) &&
                        int.TryParse(expiresElement.GetString(), out var expiresIn))
                    {
                        _tokenExpiry = DateTime.Now.AddSeconds(expiresIn - 300); // -5 минут для надежности
                    }
                    else
                    {
                        _tokenExpiry = DateTime.Now.AddHours(1);
                    }

                    Console.WriteLine("Google OAuth токен обновлен успешно");
                }
            }
            else
            {
                Console.WriteLine($"Ошибка при обновлении Google OAuth токена: {tokenResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обновлении Google OAuth токена: {ex.Message}");
        }
    }

    private async Task<string> SendChatRequestAsync(string userInput, CancellationToken cancellationToken)
    {
        try
        {
            // Сначала получаем сессию ChatGPT+ через Google OAuth
            var sessionRequest = new RestRequest("/api/auth/session", Method.Post);
            sessionRequest.AddHeader("Authorization", $"Bearer {_accessToken}");
            sessionRequest.AddHeader("Content-Type", "application/json");
            sessionRequest.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            var sessionResponse = await _openaiClient.ExecuteAsync(sessionRequest, cancellationToken);

            if (!sessionResponse.IsSuccessful)
            {
                return $"Ошибка при получении сессии ChatGPT+: {sessionResponse.StatusCode}";
            }

            // Теперь отправляем запрос к ChatGPT+
            var chatRequest = new RestRequest("/backend-api/conversation", Method.Post);
            chatRequest.AddHeader("Authorization", $"Bearer {_accessToken}");
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

            var response = await _openaiClient.ExecuteAsync(chatRequest, cancellationToken);

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
        catch (Exception ex)
        {
            return $"Ошибка при отправке запроса к ChatGPT+: {ex.Message}";
        }
    }

    public string GetProviderName() => "ChatGPT+ (Google OAuth)";

    public bool IsAvailable() => !string.IsNullOrEmpty(_config.ClientId) && 
                                 !string.IsNullOrEmpty(_config.ClientSecret) && 
                                 !string.IsNullOrEmpty(_config.RefreshToken);
}
