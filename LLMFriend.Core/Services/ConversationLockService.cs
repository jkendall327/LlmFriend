using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LLMFriend.Services;

public class ConversationLockService : IConversationLockService
{
    private readonly ILogger<ConversationLockService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string? _currentConversationSource;
    private DateTimeOffset _lastActivity = DateTimeOffset.MinValue;
    private readonly TimeSpan _conversationTimeout = TimeSpan.FromMinutes(15);

    public ConversationLockService(ILogger<ConversationLockService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> TryAcquireConversationLockAsync(string source, CancellationToken token = default)
    {
        // Check if we need to reset a stale lock
        CheckAndResetStaleLock();
        
        // Try to acquire the semaphore without waiting
        if (await _semaphore.WaitAsync(TimeSpan.Zero, token))
        {
            try
            {
                _currentConversationSource = source;
                _lastActivity = DateTimeOffset.UtcNow;
                _logger.LogInformation("Conversation lock acquired by {Source}", source);
                return true;
            }
            catch
            {
                _semaphore.Release();
                throw;
            }
        }
        
        _logger.LogInformation("Failed to acquire conversation lock - already held by {CurrentSource}", 
            _currentConversationSource ?? "unknown");
        return false;
    }

    public void ReleaseConversationLock()
    {
        if (_currentConversationSource != null)
        {
            var source = _currentConversationSource;
            _currentConversationSource = null;
            _lastActivity = DateTimeOffset.MinValue;
            
            try
            {
                _semaphore.Release();
                _logger.LogInformation("Conversation lock released by {Source}", source);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error releasing conversation lock from {Source}", source);
            }
        }
    }

    public bool IsConversationActive()
    {
        CheckAndResetStaleLock();
        return _currentConversationSource != null;
    }
    
    private void CheckAndResetStaleLock()
    {
        // If the last activity was too long ago, force release the lock
        if (_currentConversationSource != null && 
            DateTimeOffset.UtcNow - _lastActivity > _conversationTimeout)
        {
            _logger.LogWarning("Resetting stale conversation lock from {Source} after timeout", 
                _currentConversationSource);
            ReleaseConversationLock();
        }
    }
    
    // Call this method when there's activity in an ongoing conversation
    public void UpdateLastActivity()
    {
        if (_currentConversationSource != null)
        {
            _lastActivity = DateTimeOffset.UtcNow;
        }
    }
}
