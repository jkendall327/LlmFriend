using LLMFriend.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LLMFriend.Services;

public class LateReplyDetector(IOptions<AppConfiguration> config)
{
    public bool UserMessageWasLate(ChatHistory history)
    {
        var penultimate = history.Reverse().Skip(1).Take(1).Single();
        var last = history.Last();

        var penultimateTime = GetMessageTime(penultimate);
        var lastTime = GetMessageTime(last);

        var delta = lastTime.Subtract(penultimateTime);

        var expected = config.Value.TimeForExpectedReplyInConversation;

        return delta > expected;
    }

    private static DateTime GetMessageTime(ChatMessageContent message)
    {
        if (message.Metadata is null)
        {
            throw new InvalidOperationException("Message had no metadata.");
        }
        
        var found = message.Metadata.TryGetValue("time", out var time);

        if (found is not true)
        {
            throw new InvalidOperationException("'Time' field not found in metadata.");
        }
        
        if (time is null)
        {
            throw new InvalidOperationException("Null value stored in the 'time' field.");
        }
        
        var foo = (DateTime)time;
        return foo;
    }
}