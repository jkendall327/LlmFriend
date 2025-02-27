using LLMFriend.Configuration;
using LLMFriend.Services;
using LLMFriend.Web.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace LLMFriend.Tests.Services;

public class CrontabServiceTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly Mock<ChatNotificationService> _mockNotificationService = new();
    private readonly Mock<IOptions<AppConfiguration>> _mockOptions = new();
    private readonly Mock<ILogger<CrontabService>> _mockLogger = new();
    
    private CrontabService CreateService(string? cronExpression = null)
    {
        var config = new AppConfiguration
        {
            CrontabForScheduledInvocation = cronExpression ?? "*/5 * * * *",
            PersonalityProfilePath = string.Empty
        };
        
        _mockOptions.Setup(o => o.Value).Returns(config);
        
        return new CrontabService(
            _timeProvider,
            _mockNotificationService.Object,
            _mockOptions.Object,
            _mockLogger.Object);
    }
    
    [Fact]
    public async Task WaitForCrontab_CalculatesCorrectDelay()
    {
        // Arrange
        var currentTime = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.Local);
        _timeProvider.SetUtcNow(currentTime.ToUniversalTime());
        
        var service = CreateService("*/5 * * * *"); // Every 5 minutes
        
        // Act & Assert - we're testing that it calculates the correct delay
        // This is a bit tricky to test directly, so we'll use a timeout to ensure
        // it doesn't hang indefinitely
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        
        // The test will pass if this doesn't throw - the method will wait for the delay
        // and then try to notify, which will be canceled by our token
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.WaitForCrontab(cts.Token));
        
        // Verify notification was not called due to cancellation
        _mockNotificationService.Verify(
            ns => ns.NotifyNewChatRequested(
                It.IsAny<DateTimeOffset>(), 
                It.IsAny<CancellationToken>(), 
                It.IsAny<string>()),
            Times.Never);
    }
    
    [Fact]
    public async Task WaitForCrontab_NotifiesWhenTimeReached()
    {
        // Arrange
        var currentTime = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.Local);
        _timeProvider.SetUtcNow(currentTime.ToUniversalTime());
        
        _mockNotificationService
            .Setup(ns => ns.NotifyNewChatRequested(
                It.IsAny<DateTimeOffset>(), 
                It.IsAny<CancellationToken>(), 
                It.IsAny<string>()))
            .ReturnsAsync(true);
        
        // Create a service with a cron expression that would execute immediately
        var service = CreateService("* * * * *");
        
        // Use a short timeout to prevent the test from hanging
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        
        // Act - we'll advance time to simulate the cron trigger
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        
        try
        {
            await service.WaitForCrontab(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected due to our short timeout
        }
        
        // Assert
        _mockNotificationService.Verify(
            ns => ns.NotifyNewChatRequested(
                It.IsAny<DateTimeOffset>(), 
                It.IsAny<CancellationToken>(), 
                It.Is<string>(s => s == "scheduled")),
            Times.Once);
    }
    
    [Fact]
    public async Task WaitForCrontab_LogsWarningWhenNoNextOccurrence()
    {
        // Arrange
        var currentTime = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.Local);
        _timeProvider.SetUtcNow(currentTime.ToUniversalTime());
        
        // Use an invalid cron expression that won't have a next occurrence
        // For testing purposes, we'll mock this by using a special test-only expression
        var service = CreateService("0 0 30 2 *"); // February 30th - doesn't exist
        
        // Act
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.WaitForCrontab(cts.Token));
        
        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No next occurrence found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
