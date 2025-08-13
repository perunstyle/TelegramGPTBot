using System.Text.Json;
using RestSharp;
using TelegramGPTBot;

namespace TelegramGPTBot.Providers;

public class OpenRouterProvider : IChatProvider
{
    private readonly OpenRouterConfig _config;
    private readonly RestClient _client;

    public OpenRouterProvider(OpenRouterConfig config)
    {
        _config = config;
        _client = new RestClient("https://openrouter.ai/api/v1/chat/completions");
    }

    public async Task<string> GetResponseAsync(string userInput, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest();
        request.AddHeader("Authorization", $"Bearer {_config.ApiKey}");
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("HTTP-Referer", _config.HttpReferer);

        var body = new
        {
            model = _config.Model,
            messages = new[]
            {
                new { role = "user", content = userInput }
            }
        };

        request.AddJsonBody(body);

        var response = await _client.ExecutePostAsync(request, cancellationToken);
        if (!response.IsSuccessful)
            return $"Ошибка OpenRouter: {response.StatusCode} - {response.StatusDescription}";

        try
        {
            var json = JsonDocument.Parse(response.Content!);
            var content = json.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content?.Trim() ?? "Пустой ответ от OpenRouter";
        }
        catch (Exception ex)
        {
            return $"Ошибка при обработке ответа: {ex.Message}";
        }
    }

    public string GetProviderName() => "OpenRouter API";

    public bool IsAvailable() => !string.IsNullOrEmpty(_config.ApiKey);
}
