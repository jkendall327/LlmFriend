using LLMFriend.Configuration;
using LLMFriend.Services;
using LLMFriend.Web.Services;
using Microsoft.Extensions.Options;

namespace LLMFriend.Web.Services;

public class ScheduledBackgroundService(
    CrontabService crontabService,
    IChatNotificationService notificationService,
    IOptions<AppConfiguration> appConfig,
    ILogger<ScheduledBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!appConfig.Value.CanActAutonomously)
        {
            return;
        }
        
        // Start a task to monitor for crontab events
        _ = MonitorCrontabAsync(stoppingToken);
        
        // Wait for the cancellation token to be triggered
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    
    private async Task MonitorCrontabAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for the next crontab event
                await crontabService.WaitForCrontab(stoppingToken);
                
                // When a crontab event occurs, create an invocation context
                var currentTime = DateTimeOffset.UtcNow;
                var context = new InvocationContext
                {
                    InvocationTime = currentTime,
                    Type = InvocationType.Scheduled,
                    Username = "System",
                    UserStartingMessage = null
                };
                
                // Notify that a new chat is requested with this context
                logger.LogInformation("Crontab triggered - requesting new chat");
                bool started = await notificationService.NotifyNewChatRequested(
                    currentTime,
                    stoppingToken,
                    "crontab",
                    context);
                    
                if (!started)
                {
                    logger.LogInformation("Could not start scheduled conversation - another conversation is already active");
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in crontab monitoring");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }


}
