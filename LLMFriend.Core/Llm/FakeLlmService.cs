using Microsoft.SemanticKernel.ChatCompletion;

namespace LLMFriend.Services;

public class FakeLlmService : ILlmService
{
    public Task<ChatHistory> InvokeLlmAsync(InvocationContext context)
    {
        var history = new ChatHistory();
        
        history.AddAssistantMessage("Hello world!");
        
        return Task.FromResult(history);
    }

    public Task<ChatHistory> ContinueConversationAsync(ChatHistory chatHistory, ConversationContinuation details)
    {
        var number = chatHistory.Count;
        
        var content = details.MessageTookTooLong ? "Are you there?" : "Yes that's very interesting...";
        
        chatHistory.AddAssistantMessage(content + $" ({Random.Shared.Next(100).ToString()})");
        
        return Task.FromResult(chatHistory);
    }
}