using System.Threading.Channels;
using LLMFriend.Services;

namespace LLMFriend.Web.Services;

public interface IChatNotificationService
{
    ChannelReader<ChatNotification> GetReader();
    Task<bool> NotifyNewChatRequested(DateTimeOffset timestamp, CancellationToken token = default, string source = "manual", InvocationContext? context = null);
    void ReleaseConversationLock();
    bool IsConversationActive();
    void UpdateConversationActivity();
}

public class ChatNotificationService : IChatNotificationService
{
    private readonly Channel<ChatNotification> _channel = 
        Channel.CreateUnbounded<ChatNotification>();
    private readonly IConversationLockService _lockService;

    public ChatNotificationService(IConversationLockService lockService)
    {
        _lockService = lockService;
    }

    public ChannelReader<ChatNotification> GetReader() => _channel.Reader;
    
    public async Task<bool> NotifyNewChatRequested(DateTimeOffset timestamp, CancellationToken token = default, string source = "manual", InvocationContext? context = null)
    {
        // Try to acquire the conversation lock
        if (!await _lockService.TryAcquireConversationLockAsync(source, token))
        {
            return false;
        }

        try
        {
            var notification = new ChatNotification(timestamp, source, context);
            await _channel.Writer.WriteAsync(notification, token);
            return true;
        }
        catch
        {
            // If there's an error, make sure we release the lock
            _lockService.ReleaseConversationLock();
            throw;
        }
    }
    
    public void ReleaseConversationLock()
    {
        _lockService.ReleaseConversationLock();
    }
    
    public bool IsConversationActive()
    {
        return _lockService.IsConversationActive();
    }
    
    public void UpdateConversationActivity()
    {
        if (_lockService is ConversationLockService lockService)
        {
            lockService.UpdateLastActivity();
        }
    }
}
