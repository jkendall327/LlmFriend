namespace LLMFriend.Services;

public interface IConversationLockService
{
    /// <summary>
    /// Tries to acquire a lock for starting a conversation.
    /// </summary>
    /// <param name="source">The source attempting to start the conversation</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>True if the lock was acquired, false otherwise</returns>
    Task<bool> TryAcquireConversationLockAsync(string source, CancellationToken token = default);
    
    /// <summary>
    /// Releases the conversation lock.
    /// </summary>
    void ReleaseConversationLock();
    
    /// <summary>
    /// Checks if there is an active conversation.
    /// </summary>
    /// <returns>True if there is an active conversation, false otherwise</returns>
    bool IsConversationActive();
}
