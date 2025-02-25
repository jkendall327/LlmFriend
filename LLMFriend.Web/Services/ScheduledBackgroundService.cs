using Cronos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMFriend.Web.Services;

public class ScheduledBackgroundService : BackgroundService
{
    private readonly ILogger<ScheduledBackgroundService> _logger;
    private readonly TimeProvider _clock;
    private readonly CronExpression _cronExpression;

    public ScheduledBackgroundService(
        ILogger<ScheduledBackgroundService> logger,
        TimeProvider clock)
    {
        _logger = logger;
        _clock = clock;
        _cronExpression = CronExpression.Parse("*/5 * * * *"); // Every 5 minutes for testing
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
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

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Executing scheduled background work");
        // TODO: Add actual work here
        await Task.CompletedTask;
    }
}
