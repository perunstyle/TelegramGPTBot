namespace TelegramGPTBot.Providers;

public interface IChatProvider
{
    Task<string> GetResponseAsync(string userInput, CancellationToken cancellationToken = default);
    string GetProviderName();
    bool IsAvailable();
}
