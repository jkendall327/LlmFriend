namespace LLMFriend.Services;

public class ChatNotification
{
    public DateTimeOffset Timestamp { get; set; }
    public string Source { get; set; }
    public InvocationContext? Context { get; set; }

    public ChatNotification(DateTimeOffset timestamp, string source, InvocationContext? context = null)
    {
        Timestamp = timestamp;
        Source = source;
        Context = context;
    }
}
