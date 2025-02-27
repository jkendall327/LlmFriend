namespace LLMFriend.Core.Scheduling;

public interface ICrontabService
{
    /// <summary>
    /// Gets the next occurrence time based on the crontab expression
    /// </summary>
    /// <param name="currentTime">The current time</param>
    /// <returns>The next occurrence time or null if none found</returns>
    DateTimeOffset? GetNextOccurrence(DateTimeOffset currentTime);
    
    /// <summary>
    /// Calculates the delay until the next occurrence
    /// </summary>
    /// <param name="currentTime">The current time</param>
    /// <returns>TimeSpan until next occurrence or null if none found</returns>
    TimeSpan? GetDelayUntilNextOccurrence(DateTimeOffset currentTime);
}
