using System.Text.Json;

namespace TelegramGPTBot;

public static class GoogleOAuthHelper
{
    public static string GetAuthorizationUrlAsync(string clientId, string redirectUri)
    {
        var scopes = new[]
        {
            "https://www.googleapis.com/auth/userinfo.email",
            "https://www.googleapis.com/auth/userinfo.profile",
            "openid"
        };

        var scopeString = string.Join(" ", scopes);
        var state = Guid.NewGuid().ToString();

        return $"https://accounts.google.com/o/oauth2/v2/auth?" +
               $"client_id={Uri.EscapeDataString(clientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&scope={Uri.EscapeDataString(scopeString)}" +
               $"&response_type=code" +
               $"&state={state}" +
               $"&access_type=offline" +
               $"&prompt=consent";
    }

    public static async Task<GoogleOAuthTokens> ExchangeCodeForTokensAsync(
        string clientId, 
        string clientSecret, 
        string authorizationCode, 
        string redirectUri)
    {
        using var client = new HttpClient();
        
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("code", authorizationCode),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", redirectUri)
        });

        var response = await client.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Ошибка при обмене кода на токены: {response.StatusCode} - {responseContent}");
        }

        var tokens = JsonSerializer.Deserialize<GoogleOAuthTokens>(responseContent);
        
        if (tokens == null)
        {
            throw new Exception("Не удалось десериализовать ответ от Google OAuth");
        }

        return tokens;
    }
}

public class GoogleOAuthTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string Scope { get; set; } = string.Empty;
}
