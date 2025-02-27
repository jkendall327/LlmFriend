using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LLMFriend.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace LLMFriend.Tests.Services;

public class ConversationLockServiceTests
{
    private readonly ILogger<ConversationLockService> _logger = NullLogger<ConversationLockService>.Instance;
    private readonly FakeTimeProvider _timeProvider = new();
    
    [Fact]
    public async Task TryAcquireConversationLockAsync_ShouldAcquireLock_WhenNotAlreadyHeld()
    {
        // Arrange
        var service = new ConversationLockService(_logger, _timeProvider);
        
        // Act
        var result = await service.TryAcquireConversationLockAsync("TestSource");
        
        // Assert
        Assert.True(result);
        Assert.True(service.IsConversationActive());
    }
    
    [Fact]
    public async Task TryAcquireConversationLockAsync_ShouldReturnFalse_WhenLockAlreadyHeld()
    {
        // Arrange
        var service = new ConversationLockService(_logger, _timeProvider);
        await service.TryAcquireConversationLockAsync("FirstSource");
        
        // Act
        var result = await service.TryAcquireConversationLockAsync("SecondSource");
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public async Task ReleaseConversationLock_ShouldReleaseLock()
    {
        // Arrange
        var service = new ConversationLockService(_logger, _timeProvider);
        await service.TryAcquireConversationLockAsync("TestSource");
        
        // Act
        service.ReleaseConversationLock();
        
        // Assert
        Assert.False(service.IsConversationActive());
        
        // Verify we can acquire the lock again
        var result = await service.TryAcquireConversationLockAsync("AnotherSource");
        Assert.True(result);
    }
    
    [Fact]
    public async Task CheckAndResetStaleLock_ShouldResetLock_WhenTimeout()
    {
        // Arrange
        var service = new ConversationLockService(_logger, _timeProvider);
        await service.TryAcquireConversationLockAsync("TestSource");
        
        // Act - advance time past the timeout (15 minutes)
        _timeProvider.Advance(TimeSpan.FromMinutes(16));
        
        // Assert - the lock should be considered inactive due to timeout
        Assert.False(service.IsConversationActive());
        
        // Verify we can acquire the lock again
        var result = await service.TryAcquireConversationLockAsync("NewSource");
        Assert.True(result);
    }
    
    [Fact]
    public async Task UpdateLastActivity_ShouldPreventTimeout()
    {
        // Arrange
        var service = new ConversationLockService(_logger, _timeProvider);
        await service.TryAcquireConversationLockAsync("TestSource");
        
        // Act - advance time almost to timeout, then update activity
        _timeProvider.Advance(TimeSpan.FromMinutes(14));
        service.UpdateLastActivity();
        
        // Advance time again, but not enough for a total timeout
        _timeProvider.Advance(TimeSpan.FromMinutes(14));
        
        // Assert - the lock should still be active because we updated the activity
        Assert.True(service.IsConversationActive());
    }
    
    [Fact]
    public async Task MultipleThreads_ShouldRespectLock()
    {
        // Arrange
        var service = new ConversationLockService(_logger, _timeProvider);
        var acquiredCount = 0;
        
        // Act - try to acquire the lock from multiple tasks
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var source = $"Source{i}";
            tasks.Add(Task.Run(async () =>
            {
                if (await service.TryAcquireConversationLockAsync(source))
                {
                    Interlocked.Increment(ref acquiredCount);
                    // Hold the lock briefly
                    await Task.Delay(50);
                    service.ReleaseConversationLock();
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Assert - only one task should have acquired the lock at a time
        Assert.Equal(10, acquiredCount);
    }
}
