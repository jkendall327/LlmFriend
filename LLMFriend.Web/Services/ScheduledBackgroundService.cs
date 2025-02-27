using Cronos;
using LLMFriend.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMFriend.Web.Services;

public class ScheduledBackgroundService : BackgroundService
{
    private readonly ILogger<ScheduledBackgroundService> _logger;
    private readonly TimeProvider _clock;
    private readonly CronExpression _cronExpression;
    private readonly ChatNotificationService _notificationService;
    private readonly AppConfiguration _appConfig;

    public ScheduledBackgroundService(
        ILogger<ScheduledBackgroundService> logger,
        TimeProvider clock,
        ChatNotificationService notificationService,
        IOptions<AppConfiguration> appConfig)
    {
        _notificationService = notificationService;
        _logger = logger;
        _clock = clock;
        _appConfig = appConfig.Value;
        _cronExpression = CronExpression.Parse(_appConfig.CrontabForScheduledInvocation ?? "*/5 * * * *"); // Use config or default to every 5 minutes
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await WaitForCrontab(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task WaitForCrontab(CancellationToken stoppingToken)
    {
        var currentTime = _clock.GetLocalNow();
        var nextUtc = _cronExpression.GetNextOccurrence(currentTime, TimeZoneInfo.Local);

        if (nextUtc.HasValue)
        {
            var delay = nextUtc.Value - currentTime;
            _logger.LogInformation("Next scheduled execution at {NextTime}", nextUtc.Value.ToLocalTime());
                    
            await Task.Delay(delay, stoppingToken);
                    
            if (!stoppingToken.IsCancellationRequested)
            {
                await DoWorkAsync(stoppingToken);
            }
        }
        else
        {
            _logger.LogWarning("No next occurrence found for the crontab expression");
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled background work triggered");
        
        if (!_appConfig.CanActAutonomously)
        {
            _logger.LogInformation("Autonomous actions are disabled in configuration, skipping scheduled conversation");
            return;
        }
        
        _logger.LogInformation("Attempting to initiate scheduled conversation");
        bool started = await _notificationService.NotifyNewChatRequested(
            _clock.GetLocalNow(), 
            stoppingToken,
            "scheduled");
            
        if (!started)
        {
            _logger.LogInformation("Could not start scheduled conversation - another conversation is already active");
        }
    }
}
