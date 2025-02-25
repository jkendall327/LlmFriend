namespace LLMFriend.Services;

public class InvocationContext
{
    public DateTimeOffset InvocationTime { get; set; }
    public InvocationType Type { get; set; }
    public required string Username { get; set; }
    public string[] FileList { get; set; } = [];
    public string? UserStartingMessage { get; set; }
}