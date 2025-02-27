using LLMFriend.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMFriend.Services;

public class RandomConversationService : IRandomConversationService
{
    private readonly ILogger<RandomConversationService> _logger;
    private readonly AppConfiguration _appConfig;
    private readonly Random _random;

    public RandomConversationService(
        ILogger<RandomConversationService> logger,
        IOptions<AppConfiguration> appConfig)
    {
        _logger = logger;
        _appConfig = appConfig.Value;
        _random = new Random();
    }

    public bool ShouldStartConversation(DateTimeOffset currentTime)
    {
        // Don't start conversations if autonomous actions are disabled
        if (!_appConfig.CanActAutonomously)
        {
            _logger.LogDebug("Autonomous actions are disabled, not starting random conversation");
            return false;
        }

        // Don't start conversations during typical sleeping hours (11 PM - 7 AM)
        var hour = currentTime.Hour;
        if (hour >= 23 || hour < 7)
        {
            _logger.LogDebug("Outside of active hours ({CurrentHour}), not starting random conversation", hour);
            return false;
        }

        // Use the configured probability to determine if we should start a conversation
        var roll = _random.NextDouble();
        var shouldStart = roll < _appConfig.ProbabilityOfStartingConversationsAutonomously;
        
        _logger.LogDebug(
            "Random conversation check: probability={Probability}, roll={Roll}, shouldStart={ShouldStart}", 
            _appConfig.ProbabilityOfStartingConversationsAutonomously,
            roll,
            shouldStart);
            
        return shouldStart;
    }
}
