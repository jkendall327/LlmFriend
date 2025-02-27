using Cronos;
using LLMFriend.Configuration;
using LLMFriend.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMFriend.Services;

public class CrontabService(
    TimeProvider clock,
    IChatNotificationService notificationService,
    IOptions<AppConfiguration> configuration,
    ILogger<CrontabService> logger)
{
    private readonly CronExpression _cronExpression =
        CronExpression.Parse(configuration.Value.CrontabForScheduledInvocation ?? "*/5 * * * *"); 

    public async Task WaitForCrontab(CancellationToken stoppingToken)
    {
        var currentTime = clock.GetLocalNow();
        var nextUtc = _cronExpression.GetNextOccurrence(currentTime, TimeZoneInfo.Local);

        if (nextUtc.HasValue)
        {
            var delay = nextUtc.Value - currentTime;
            logger.LogInformation("Next scheduled execution at {NextTime}", nextUtc.Value.ToLocalTime());

            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                await DoWorkAsync(stoppingToken);
            }
        }
        else
        {
            logger.LogWarning("No next occurrence found for the crontab expression");
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Scheduled background work triggered");

        logger.LogInformation("Attempting to initiate scheduled conversation");

        bool started = await notificationService.NotifyNewChatRequested(
            clock.GetLocalNow(),
            stoppingToken,
            "scheduled");

        if (!started)
        {
            logger.LogInformation("Could not start scheduled conversation - another conversation is already active");
        }
    }
}