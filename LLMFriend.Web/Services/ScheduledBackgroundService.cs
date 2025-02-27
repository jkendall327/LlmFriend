using LLMFriend.Core.Scheduling;

namespace LLMFriend.Web.Services;

public class ScheduledBackgroundService : BackgroundService
{
    private readonly ILogger<ScheduledBackgroundService> _logger;
    private readonly TimeProvider _clock;
    private readonly ChatNotificationService _notificationService;
    private readonly IScheduledWorkService _scheduledWorkService;

    public ScheduledBackgroundService(
        ILogger<ScheduledBackgroundService> logger,
        TimeProvider clock,
        ChatNotificationService _notificationService,
        IScheduledWorkService scheduledWorkService)
    {
        this._notificationService = _notificationService;
        _logger = logger;
        _clock = clock;
        _scheduledWorkService = scheduledWorkService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var currentTime = _clock.GetLocalNow();
                
                // Wait until the next scheduled execution time
                await _scheduledWorkService.WaitForNextExecutionAsync(currentTime, stoppingToken);
                
                if (!stoppingToken.IsCancellationRequested)
                {
                    // Execute the scheduled work
                    bool shouldInitiateConversation = await _scheduledWorkService.ExecuteScheduledWorkAsync(
                        _clock.GetLocalNow(), 
                        stoppingToken);
                    
                    if (shouldInitiateConversation)
                    {
                        await InitiateScheduledConversationAsync(stoppingToken);
                    }
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
    
    private async Task InitiateScheduledConversationAsync(CancellationToken stoppingToken)
    {
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
