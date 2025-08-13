using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using RestSharp;
using TelegramGPTBot;
using TelegramGPTBot.Providers;

internal class Program
{
    static async Task Main()
    {
        // Настройка конфигурации
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var botConfig = new BotConfiguration();
        configuration.Bind(botConfig);

        // Проверка наличия обязательных ключей
        if (string.IsNullOrEmpty(botConfig.TelegramBot.BotToken))
        {
            Console.WriteLine("Ошибка: Telegram Bot Token не найден в конфигурации!");
            Console.WriteLine("Пожалуйста, создайте файл appsettings.json с вашими ключами API.");
            return;
        }

        // Создаем фабрику провайдеров
        ChatProviderFactory providerFactory;
        try
        {
            providerFactory = new ChatProviderFactory(botConfig);
            Console.WriteLine($"Доступные провайдеры: {string.Join(", ", providerFactory.GetAvailableProviders())}");
            Console.WriteLine($"Текущий провайдер: {providerFactory.GetCurrentProviderName()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при инициализации провайдеров: {ex.Message}");
            return;
        }

        ITelegramBotClient botClient = new TelegramBotClient(botConfig.TelegramBot.BotToken);
        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        var updateHandler = new DefaultUpdateHandler(
            async (bot, update, token) =>
                await HandleUpdateAsync(bot, update, token, providerFactory),
            HandlePollingErrorAsync
        );

        botClient.StartReceiving(
            updateHandler: updateHandler,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Бот {me.Username} в работе");
        Console.ReadLine();
        cts.Cancel();
    }

    static async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken,
        ChatProviderFactory providerFactory
    )
    {
        if (update.Type != UpdateType.Message || update.Message?.Text is null) //надо будет идент на юзера добавить
            return;

        var chatId = update.Message.Chat.Id;
        var userMessage = update.Message.Text;

        Console.WriteLine($"Пользователь {chatId}: {userMessage}");

        // Обработка команд управления провайдерами
        if (userMessage.StartsWith("/"))
        {
            string commandReply = await HandleCommandAsync(userMessage, providerFactory);
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: commandReply,
                cancellationToken: cancellationToken
            );
            return;
        }

        // Получаем ответ от текущего провайдера
        var provider = providerFactory.GetProvider();
        string reply = await provider.GetResponseAsync(userMessage, cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: reply,
            cancellationToken: cancellationToken
        );
    }

    static Task HandlePollingErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}] {apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine($"Ошибка: {errorMessage}");
        return Task.CompletedTask;
    }

    static Task<string> HandleCommandAsync(string command, ChatProviderFactory providerFactory)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLower();

        switch (cmd)
        {
            case "/providers":
                var availableProviders = providerFactory.GetAvailableProviders();
                return Task.FromResult($"Доступные провайдеры:\n{string.Join("\n", availableProviders)}");

            case "/current":
                var currentProvider = providerFactory.GetCurrentProviderName();
                return Task.FromResult($"Текущий провайдер: {currentProvider}");

            case "/switch":
                if (parts.Length < 2)
                    return Task.FromResult("Использование: /switch <имя_провайдера>\nПример: /switch ChatGPTPlus");
                
                var targetProvider = parts[1];
                providerFactory.SwitchProvider(targetProvider);
                return Task.FromResult($"Переключен на провайдер: {targetProvider}");

            case "/help":
                return Task.FromResult(@"Доступные команды:
/providers - показать доступные провайдеры
/current - показать текущий провайдер
/switch <провайдер> - переключиться на другой провайдер
/help - показать эту справку");

            default:
                return Task.FromResult($"Неизвестная команда: {cmd}\nИспользуйте /help для справки");
        }
    }

    static async Task<string> AskOpenRouter(string userInput, OpenRouterConfig config)
    {
        var client = new RestClient("https://openrouter.ai/api/v1/chat/completions");
        var request = new RestRequest();

        request.AddHeader("Authorization", $"Bearer {config.ApiKey}");
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("HTTP-Referer", config.HttpReferer);

        var body = new
        {
            model = config.Model,
            messages = new[]
            {
                new { role = "user", content = userInput }
            }
        };

        request.AddJsonBody(body);

        var response = await client.ExecutePostAsync(request);
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
}
