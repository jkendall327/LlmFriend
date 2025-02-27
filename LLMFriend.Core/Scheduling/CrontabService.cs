using Cronos;
using LLMFriend.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMFriend.Core.Scheduling;

public class CrontabService : ICrontabService
{
    private readonly ILogger<CrontabService> _logger;
    private readonly CronExpression _cronExpression;
    private readonly TimeZoneInfo _timeZone;

    public CrontabService(
        ILogger<CrontabService> logger,
        IOptions<AppConfiguration> appConfig)
    {
        _logger = logger;
        _timeZone = TimeZoneInfo.Local;
        
        var cronExpression = appConfig.Value.CrontabForScheduledInvocation ?? "*/5 * * * *";
        _cronExpression = CronExpression.Parse(cronExpression);
        
        _logger.LogInformation("Initialized crontab service with expression: {CronExpression}", cronExpression);
    }

    public DateTimeOffset? GetNextOccurrence(DateTimeOffset currentTime)
    {
        var nextUtc = _cronExpression.GetNextOccurrence(currentTime, _timeZone);
        
        if (!nextUtc.HasValue)
        {
            _logger.LogWarning("No next occurrence found for the crontab expression");
            return null;
        }
        
        return nextUtc.Value;
    }

    public TimeSpan? GetDelayUntilNextOccurrence(DateTimeOffset currentTime)
    {
        var nextOccurrence = GetNextOccurrence(currentTime);
        
        if (!nextOccurrence.HasValue)
        {
            return null;
        }
        
        return nextOccurrence.Value - currentTime;
    }
}
