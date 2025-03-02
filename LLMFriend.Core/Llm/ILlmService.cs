using Microsoft.SemanticKernel.ChatCompletion;

namespace LLMFriend.Services
{
    public interface ILlmService
    {
        Task<ChatHistory> InvokeLlmAsync(InvocationContext context);
        Task<ChatHistory> ContinueConversationAsync(ChatHistory chatHistory, ConversationContinuation details);
    }
}
