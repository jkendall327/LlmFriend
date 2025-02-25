using System;
using Cronos;
using LLMFriend.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMFriend.Services
{
    public class SchedulingService : ISchedulingService
    {
        private readonly ConfigurationModel _config;
        private readonly TimeProvider _clock;
        private readonly ILogger<SchedulingService> _logger;
        private readonly CronExpression _cronExpression;

        public SchedulingService(IOptionsMonitor<ConfigurationModel> config, TimeProvider clock, ILogger<SchedulingService> logger)
        {
            _config = config.CurrentValue;
            _clock = clock;
            _logger = logger;

            try
            {
                _cronExpression = CronExpression.Parse(_config.CrontabForScheduledInvocation);
            }
            catch (Exception ex)
            {
                _logger.LogError("Invalid crontab expression: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public DateTimeOffset GetNextInvocationTime()
        {
            try
            {
                var currentTime = _clock.GetLocalNow();
                var nextUtc = _cronExpression.GetNextOccurrence(currentTime, TimeZoneInfo.Local);

                if (nextUtc.HasValue)
                {
                    return nextUtc.Value.ToLocalTime();
                }
                else
                {
                    _logger.LogWarning("No next occurrence found for the crontab expression.");
                    return DateTime.MinValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error calculating next invocation time: {ErrorMessage}", ex.Message);
                return DateTime.MinValue;
            }
        }
    }
}
