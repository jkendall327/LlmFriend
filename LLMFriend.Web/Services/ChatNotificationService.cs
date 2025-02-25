using System.Threading.Channels;

namespace LLMFriend.Web.Services;

public class ChatNotificationService
{
    private readonly Channel<DateTime> _channel;

    public ChatNotificationService()
    {
        _channel = Channel.CreateUnbounded<DateTime>();
    }

    public ChannelReader<DateTime> GetReader() => _channel.Reader;
    
    public async Task NotifyNewChatRequested(DateTime timestamp)
    {
        await _channel.Writer.WriteAsync(timestamp);
    }
}
