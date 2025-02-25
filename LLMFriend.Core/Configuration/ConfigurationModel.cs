using System;

namespace LLMFriend.Configuration
{
    public class ConfigurationModel
    {
        public bool EnableToolUse { get; set; }
        public string[] AllowedFilePathsForSearch { get; set; }
        public string CrontabForScheduledInvocation { get; set; }
        public double ProbabilityOfStartingConversationsAutonomously { get; set; }
        public TimeSpan TimeForExpectedReplyInConversation { get; set; }
        public bool AutonomousFeaturesEnabled { get; set; }
        public string PersonalityProfilePath { get; set; }
        public string? MemoryBankFolder { get; set; }

        // Added for Semantic Kernel integration
        public string OpenAIApiKey { get; set; }
    }
}
