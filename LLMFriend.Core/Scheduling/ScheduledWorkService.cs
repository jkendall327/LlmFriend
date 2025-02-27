using LLMFriend.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMFriend.Core.Scheduling;

public class ScheduledWorkService : IScheduledWorkService
{
    private readonly ILogger<ScheduledWorkService> _logger;
    private readonly ICrontabService _crontabService;
    private readonly AppConfiguration _appConfig;

    public ScheduledWorkService(
        ILogger<ScheduledWorkService> logger,
        ICrontabService crontabService,
        IOptions<AppConfiguration> appConfig)
    {
        _logger = logger;
        _crontabService = crontabService;
        _appConfig = appConfig.Value;
    }

    public async Task<bool> ExecuteScheduledWorkAsync(DateTimeOffset currentTime, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduled background work triggered");
        
        if (!_appConfig.CanActAutonomously)
        {
            _logger.LogInformation("Autonomous actions are disabled in configuration, skipping scheduled conversation");
            return false;
        }
        
        return true;
    }

    public async Task WaitForNextExecutionAsync(DateTimeOffset currentTime, CancellationToken cancellationToken)
    {
        var nextOccurrence = _crontabService.GetNextOccurrence(currentTime);
        var delay = _crontabService.GetDelayUntilNextOccurrence(currentTime);

        if (nextOccurrence.HasValue && delay.HasValue)
        {
            _logger.LogInformation("Next scheduled execution at {NextTime}", nextOccurrence.Value.ToLocalTime());
            await Task.Delay(delay.Value, cancellationToken);
        }
        else
        {
            _logger.LogWarning("No next occurrence found for the crontab expression");
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
    }
}
