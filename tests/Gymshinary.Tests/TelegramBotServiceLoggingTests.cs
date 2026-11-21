using Gymshinary.Bot.Services;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Xunit;

namespace Gymshinary.Tests;

public class TelegramBotServiceLoggingTests
{
    [Fact]
    public async Task HandleUpdateAsync_DoesNotLogRawMessageText_WhenSensitiveMessageReceived()
    {
        var botClient = new TestTelegramBotClient();
        var logger = new TestLogger<TelegramBotService>();
        var service = new TelegramBotService(botClient, logger);

        const string sensitiveWorkoutText = "Жим лежа 100кг на 5 повторений";
        var update = new Update
        {
            Message = new Message
            {
                Id = 42,
                Text = sensitiveWorkoutText,
                Chat = new Chat { Id = 987654321 }
            }
        };

        await service.HandleUpdateAsync(botClient, update, CancellationToken.None);

        Assert.NotEmpty(logger.Entries);
        Assert.DoesNotContain(logger.Entries, entry => entry.Contains(sensitiveWorkoutText));

        var sentRequest = Assert.Single(botClient.SentRequests);
        var sendMessageRequest = Assert.IsType<SendMessageRequest>(sentRequest);
        Assert.Equal(987654321, sendMessageRequest.ChatId.Identifier);
        Assert.Contains("Неизвестная команда", sendMessageRequest.Text);
    }

    [Fact]
    public async Task HandleUpdateAsync_SendsExpectedResponse_ForStartCommand()
    {
        var botClient = new TestTelegramBotClient();
        var logger = new TestLogger<TelegramBotService>();
        var service = new TelegramBotService(botClient, logger);

        var update = new Update
        {
            Message = new Message
            {
                Id = 1,
                Text = "/start",
                Chat = new Chat { Id = 12345 }
            }
        };

        await service.HandleUpdateAsync(botClient, update, CancellationToken.None);

        var sentRequest = Assert.Single(botClient.SentRequests);
        var sendMessageRequest = Assert.IsType<SendMessageRequest>(sentRequest);
        Assert.Equal(12345, sendMessageRequest.ChatId.Identifier);
        Assert.Contains("Добро пожаловать", sendMessageRequest.Text);
    }

    [Fact]
    public async Task HandleUpdateAsync_SendsExpectedResponse_ForHelpCommand()
    {
        var botClient = new TestTelegramBotClient();
        var logger = new TestLogger<TelegramBotService>();
        var service = new TelegramBotService(botClient, logger);

        var update = new Update
        {
            Message = new Message
            {
                Id = 2,
                Text = "/help",
                Chat = new Chat { Id = 12345 }
            }
        };

        await service.HandleUpdateAsync(botClient, update, CancellationToken.None);

        var sentRequest = Assert.Single(botClient.SentRequests);
        var sendMessageRequest = Assert.IsType<SendMessageRequest>(sentRequest);
        Assert.Equal(12345, sendMessageRequest.ChatId.Identifier);
        Assert.Contains("Доступные команды", sendMessageRequest.Text);
    }
}
