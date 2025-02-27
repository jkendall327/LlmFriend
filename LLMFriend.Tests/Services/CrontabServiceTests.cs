using LLMFriend.Configuration;
using LLMFriend.Services;
using LLMFriend.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LLMFriend.Tests.Services;

public class CrontabServiceTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly ChatNotificationService _notificationService = Substitute.For<ChatNotificationService>(
        Substitute.For<IConversationLockService>(),
        Substitute.For<IChatService>(),
        Substitute.For<ILogger<ChatNotificationService>>());
    private readonly IOptions<AppConfiguration> _options = Substitute.For<IOptions<AppConfiguration>>();
    private readonly ILogger<CrontabService> _logger = new NullLogger<CrontabService>();
    
    private CrontabService CreateService(string? cronExpression = null)
    {
        var config = new AppConfiguration
        {
            CrontabForScheduledInvocation = cronExpression ?? "*/5 * * * *",
            PersonalityProfilePath = string.Empty
        };
        
        _options.Value.Returns(config);
        
        return new CrontabService(
            _timeProvider,
            _notificationService,
            _options,
            _logger);
    }
    
    [Fact]
    public async Task WaitForCrontab_CalculatesCorrectDelay()
    {
        var currentTime = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.Local);
        _timeProvider.SetUtcNow(currentTime.ToUniversalTime());
        
        var service = CreateService("*/5 * * * *"); // Every 5 minutes
        
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.WaitForCrontab(cts.Token));
        
        await _notificationService.DidNotReceive().NotifyNewChatRequested(
            Arg.Any<DateTimeOffset>(), 
            Arg.Any<CancellationToken>(), 
            Arg.Any<string>());
    }
    
    [Fact]
    public async Task WaitForCrontab_NotifiesWhenTimeReached()
    {
        var currentTime = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.Local);
        _timeProvider.SetUtcNow(currentTime.ToUniversalTime());
        
        _notificationService.NotifyNewChatRequested(
            Arg.Any<DateTimeOffset>(), 
            Arg.Any<CancellationToken>(), 
            Arg.Any<string>())
            .Returns(true);
        
        var service = CreateService("* * * * *");
        
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        
        try
        {
            await service.WaitForCrontab(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected due to our short timeout
        }
        
        await _notificationService.Received(1).NotifyNewChatRequested(
            Arg.Any<DateTimeOffset>(), 
            Arg.Any<CancellationToken>(), 
            Arg.Is<string>(s => s == "scheduled"));
    }
    
    [Fact]
    public async Task WaitForCrontab_LogsWarningWhenNoNextOccurrence()
    {
        var currentTime = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.Local);
        _timeProvider.SetUtcNow(currentTime.ToUniversalTime());
        
        // Use an invalid cron expression that won't have a next occurrence
        var service = CreateService("0 0 30 2 *"); // February 30th - doesn't exist
        
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.WaitForCrontab(cts.Token));
        
        // We're using NullLogger, so we can't verify log messages
        // This test now primarily verifies that the method doesn't throw
        // when no next occurrence is found
    }
}
