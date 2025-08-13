namespace TelegramGPTBot;

public class BotConfiguration
{
    public TelegramBotConfig TelegramBot { get; set; } = new();
    public OpenRouterConfig OpenRouter { get; set; } = new();
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
