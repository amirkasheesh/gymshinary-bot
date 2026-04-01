using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
namespace Gymshinary.Bot.Services;

public class TelegramBotService : BackgroundService
{
    private ITelegramBotClient _botClient;
    private ILogger<TelegramBotService> _logger;
    public TelegramBotService(ITelegramBotClient botClient, ILogger<TelegramBotService> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var commands = new[]
        {
            new BotCommand {Command = "start", Description = "Начало работы"},
            new BotCommand {Command = "help", Description = "Список доступных команд"}
        };
        await _botClient.SetMyCommands(commands, cancellationToken: stoppingToken);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] {UpdateType.Message}
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken token)
    {
        if (update.Message == null || update.Message.Text == null)
        {
            return;
        }
        var messageText = update.Message.Text;
        var chatId = update.Message.Chat.Id;

        _logger.LogInformation($"Получено сообщение: '{messageText}' от пользователя {chatId}");

        switch (messageText)
        {
            case "/start":
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Добро пожаловать в амбициозный проект по ведению дневника в тренажерке!",
                    cancellationToken: token
                );
                break;
            case "/help":
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Доступные команды:\n/start – начало работы",
                    cancellationToken: token
                );
                break;
            default:
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Неизвестная команда. Воспользуйтесь /help, чтобы посмотреть список команд!",
                    cancellationToken: token
                );
                break;
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
    {
        if (exception is OperationCanceledException)
        {
            return Task.CompletedTask;;
        }
        _logger.LogInformation(exception, "Ошибка! Источник: {ErrorSource}", source);

        return Task.CompletedTask;
    }
}