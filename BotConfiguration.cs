namespace TelegramGPTBot;

public class BotConfiguration
{
    public TelegramBotConfig TelegramBot { get; set; } = new();
    public OpenRouterConfig OpenRouter { get; set; } = new();
    public ChatGPTPlusConfig ChatGPTPlus { get; set; } = new();
    public GoogleOAuthConfig GoogleOAuth { get; set; } = new();
    public ChatProviderConfig ChatProvider { get; set; } = new();
}

public class TelegramBotConfig
{
    public string BotToken { get; set; } = string.Empty;
}

public class OpenRouterConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "mistralai/mistral-7b-instruct";
    public string HttpReferer { get; set; } = "https://yourapp.com";
}

public class ChatGPTPlusConfig
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ChatProviderConfig
{
    public string DefaultProvider { get; set; } = "OpenRouter";
    public bool EnableProviderSwitching { get; set; } = true;
}

public class GoogleOAuthConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost:8080/callback";
}
