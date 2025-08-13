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

        if (string.IsNullOrEmpty(botConfig.OpenRouter.ApiKey))
        {
            Console.WriteLine("Ошибка: OpenRouter API Key не найден в конфигурации!");
            Console.WriteLine("Пожалуйста, создайте файл appsettings.json с вашими ключами API.");
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
                await HandleUpdateAsync(bot, update, token, botConfig.OpenRouter),
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
        OpenRouterConfig openRouterConfig
    )
    {
        if (update.Type != UpdateType.Message || update.Message?.Text is null) //надо будет идент на юзера добавить
            return;

        var chatId = update.Message.Chat.Id;
        var userMessage = update.Message.Text;

        Console.WriteLine($"Пользователь {chatId}: {userMessage}");

        string reply = await AskOpenRouter(userMessage, openRouterConfig);

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
