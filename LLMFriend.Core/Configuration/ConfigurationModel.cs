namespace LLMFriend.Configuration;

public class ConfigurationModel
{
    public string[] AllowedFilePathsForSearch { get; set; } = [];
    public string CrontabForScheduledInvocation { get; set; } = string.Empty;
    public double ProbabilityOfStartingConversationsAutonomously { get; set; }
    public TimeSpan TimeForExpectedReplyInConversation { get; set; }
    public bool AutonomousFeaturesEnabled { get; set; }
    public string PersonalityProfilePath { get; set; } = string.Empty;
    public string? MemoryBankFolder { get; set; }
}
