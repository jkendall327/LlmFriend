using System.Threading.Channels;
using LLMFriend.Services;

namespace LLMFriend.Web.Services;

public interface IChatNotificationService
{
    ChannelReader<(DateTimeOffset timestamp, string source)> GetReader();
    Task<bool> NotifyNewChatRequested(DateTimeOffset timestamp, CancellationToken token = default, string source = "manual");
    void ReleaseConversationLock();
    bool IsConversationActive();
    void UpdateConversationActivity();
}

public class ChatNotificationService : IChatNotificationService
{
    private readonly Channel<(DateTimeOffset timestamp, string source)> _channel = 
        Channel.CreateUnbounded<(DateTimeOffset, string)>();
    private readonly IConversationLockService _lockService;

    public ChatNotificationService(IConversationLockService lockService)
    {
        _lockService = lockService;
    }

    public ChannelReader<(DateTimeOffset timestamp, string source)> GetReader() => _channel.Reader;
    
    public async Task<bool> NotifyNewChatRequested(DateTimeOffset timestamp, CancellationToken token = default, string source = "manual")
    {
        // Try to acquire the conversation lock
        if (!await _lockService.TryAcquireConversationLockAsync(source, token))
        {
            return false;
        }

        try
        {
            await _channel.Writer.WriteAsync((timestamp, source), token);
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
