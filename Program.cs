using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using RestSharp;

internal class Program
{
    static async Task Main()
    {
        string botToken = "7940397896:AAFEA5Mp5kjhy2oFsmlD3H5aa51whwuX2vo";       
        string openRouterApiKey = "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"; 
        string model = "mistralai/mistral-7b-instruct";       

        ITelegramBotClient botClient = new TelegramBotClient(botToken);
        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        var updateHandler = new DefaultUpdateHandler(
            async (bot, update, token) =>
                await HandleUpdateAsync(bot, update, token, openRouterApiKey, model),
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
        string apiKey,
        string model
    )
    {
        if (update.Type != UpdateType.Message || update.Message?.Text is null) //надо будет идент на юзера добавить
            return;

        var chatId = update.Message.Chat.Id;
        var userMessage = update.Message.Text;

        Console.WriteLine($"Пользователь {chatId}: {userMessage}");

        string reply = await AskOpenRouter(userMessage, apiKey, model);

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

    static async Task<string> AskOpenRouter(string userInput, string apiKey, string model)
    {
        var client = new RestClient("https://openrouter.ai/api/v1/chat/completions");
        var request = new RestRequest();

        request.AddHeader("Authorization", $"Bearer {apiKey}");
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("HTTP-Referer", "https://yourapp.com"); // ← можно любой URL

        var body = new
        {
            model = model,
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
