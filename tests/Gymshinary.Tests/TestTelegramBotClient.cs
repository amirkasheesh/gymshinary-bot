using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace Gymshinary.Tests;

public class TestTelegramBotClient : ITelegramBotClient
{
    public bool LocalBotServer => false;
    public long BotId => 123456789;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public IExceptionParser ExceptionsParser { get; set; } = null!;
    #pragma warning disable CS0067
    public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest;
    public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived;
    #pragma warning restore CS0067

    public List<IRequest> SentRequests { get; } = new();

    public Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        SentRequests.Add(request);
        if (typeof(TResponse) == typeof(Message))
        {
            var msg = new Message { Id = 1, Date = DateTime.UtcNow, Chat = new Chat { Id = 12345 } };
            return Task.FromResult((TResponse)(object)msg);
        }
        return Task.FromResult(default(TResponse)!);
    }

    public Task<bool> TestApi(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task DownloadFile(string filePath, Stream destination, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DownloadFile(TGFile file, Stream destination, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
