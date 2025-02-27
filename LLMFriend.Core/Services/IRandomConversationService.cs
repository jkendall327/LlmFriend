using System;

namespace LLMFriend.Services;

public interface IRandomConversationService
{
    /// <summary>
    /// Determines whether a conversation should be started at the current time
    /// based on configured probability and other factors.
    /// </summary>
    /// <param name="currentTime">The current time to evaluate</param>
    /// <returns>True if a conversation should be started, false otherwise</returns>
    bool ShouldStartConversation(DateTimeOffset currentTime);
}
