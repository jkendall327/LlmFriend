using Microsoft.Extensions.Logging;

namespace LLMFriend.Core.Scheduling;

public interface IScheduledWorkService
{
    /// <summary>
    /// Executes the scheduled work
    /// </summary>
    /// <param name="currentTime">The current time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if work was executed successfully, false otherwise</returns>
    Task<bool> ExecuteScheduledWorkAsync(DateTimeOffset currentTime, CancellationToken cancellationToken);
    
    /// <summary>
    /// Waits until the next scheduled execution time
    /// </summary>
    /// <param name="currentTime">The current time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task that completes when it's time to execute the work</returns>
    Task WaitForNextExecutionAsync(DateTimeOffset currentTime, CancellationToken cancellationToken);
}
