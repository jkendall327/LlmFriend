namespace LLMFriend.Configuration;

public class AiModelOptions
{
    public const string ConfigurationSection = "AiModel";
    
    public string ModelName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool SupportsToolUse { get; set; }
    public string? ApiRootUrl { get; set; }
}
