namespace LLMFriend.Configuration;

public class AppConfiguration
{
    public string[] AllowedFilePathsForSearch { get; init; } = [];
    public string? CrontabForScheduledInvocation { get; set; }
    public TimeSpan TimeForExpectedReplyInConversation { get; init; }
    public bool CanActAutonomously { get; set; }
    public double ProbabilityOfStartingConversationsAutonomously { get; init; }
    public required string PersonalityProfilePath { get; init; }
    public string? MemoryBankFolder { get; init; }
}
