using System.Threading.Channels;

namespace LLMFriend.Web.Services;

public class ChatNotificationService
{
    private readonly Channel<DateTimeOffset> _channel = Channel.CreateUnbounded<DateTimeOffset>();

    public ChannelReader<DateTimeOffset> GetReader() => _channel.Reader;
    
    public async Task NotifyNewChatRequested(DateTimeOffset timestamp, CancellationToken token = default)
    {
        await _channel.Writer.WriteAsync(timestamp, token);
    }
}
