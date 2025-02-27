using LLMFriend.Configuration;
using LLMFriend.Services;
using Microsoft.Extensions.Options;

namespace LLMFriend.Web.Services;

public class ScheduledBackgroundService(
    CrontabService crontabService,
    IOptions<AppConfiguration> appConfig,
    ILogger<ScheduledBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!appConfig.Value.CanActAutonomously)
        {
            return;
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await crontabService.WaitForCrontab(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in background service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }


}
