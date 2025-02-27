using LLMFriend.Configuration;
using LLMFriend.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMFriend.Web.Services;

public class RandomConversationBackgroundService : BackgroundService
{
    private readonly ILogger<RandomConversationBackgroundService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IRandomConversationService _randomConversationService;
    private readonly ChatNotificationService _notificationService;
    private readonly AppConfiguration _appConfig;

    public RandomConversationBackgroundService(
        ILogger<RandomConversationBackgroundService> logger,
        TimeProvider timeProvider,
        IRandomConversationService randomConversationService,
        ChatNotificationService notificationService,
        IOptions<AppConfiguration> appConfig)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _randomConversationService = randomConversationService;
        _notificationService = notificationService;
        _appConfig = appConfig.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Random conversation background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var currentTime = _timeProvider.GetUtcNow();
                
                if (_randomConversationService.ShouldStartConversation(currentTime))
                {
                    _logger.LogInformation("Initiating random conversation");
                    await _notificationService.NotifyNewChatRequested(currentTime, stoppingToken);
                }
                
                // Check every 15-30 minutes (randomized to avoid predictable patterns)
                var delayMinutes = Random.Shared.Next(15, 31);
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in random conversation background service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
