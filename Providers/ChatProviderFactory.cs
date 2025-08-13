using TelegramGPTBot;

namespace TelegramGPTBot.Providers;

public class ChatProviderFactory
{
    private readonly Dictionary<string, IChatProvider> _providers;
    private string _defaultProvider;

    public ChatProviderFactory(BotConfiguration config)
    {
        _providers = new Dictionary<string, IChatProvider>();
        _defaultProvider = config.ChatProvider?.DefaultProvider ?? "OpenRouter";

        // Регистрируем провайдеры
        if (config.OpenRouter != null)
        {
            _providers["OpenRouter"] = new OpenRouterProvider(config.OpenRouter);
        }

        if (config.ChatGPTPlus != null)
        {
            _providers["ChatGPTPlus"] = new ChatGPTPlusProvider(config.ChatGPTPlus);
        }

        // Проверяем доступность провайдеров
        var availableProviders = _providers.Where(p => p.Value.IsAvailable()).ToList();
        
        if (!availableProviders.Any())
        {
            throw new InvalidOperationException("Нет доступных провайдеров чата!");
        }

        // Если дефолтный провайдер недоступен, выбираем первый доступный
        if (!_providers.ContainsKey(_defaultProvider) || !_providers[_defaultProvider].IsAvailable())
        {
            _defaultProvider = availableProviders.First().Key;
            Console.WriteLine($"Внимание: Дефолтный провайдер недоступен. Используется: {_defaultProvider}");
        }
    }

    public IChatProvider GetProvider(string? providerName = null)
    {
        var name = providerName ?? _defaultProvider;
        
        if (_providers.TryGetValue(name, out var provider) && provider.IsAvailable())
        {
            return provider;
        }

        // Если запрошенный провайдер недоступен, возвращаем дефолтный
        Console.WriteLine($"Провайдер {name} недоступен. Используется дефолтный: {_defaultProvider}");
        return _providers[_defaultProvider];
    }

    public IEnumerable<string> GetAvailableProviders()
    {
        return _providers.Where(p => p.Value.IsAvailable()).Select(p => p.Key);
    }

    public string GetCurrentProviderName()
    {
        return _defaultProvider;
    }

    public void SwitchProvider(string providerName)
    {
        if (_providers.ContainsKey(providerName) && _providers[providerName].IsAvailable())
        {
            _defaultProvider = providerName;
            Console.WriteLine($"Переключен на провайдер: {providerName}");
        }
        else
        {
            Console.WriteLine($"Провайдер {providerName} недоступен!");
        }
    }
}
