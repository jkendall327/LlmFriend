using System;

namespace LLMFriend.Configuration
{
    public class ConfigurationModel
    {
        public string[] AllowedFilePathsForSearch { get; set; }
        public string CrontabForScheduledInvocation { get; set; }
        public double ProbabilityOfStartingConversationsAutonomously { get; set; }
        public TimeSpan TimeForExpectedReplyInConversation { get; set; }
        public bool AutonomousFeaturesEnabled { get; set; }
    }
}
