using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LLMFriend.Services
{
    public interface ILlmService
    {
        Task<ChatHistory> InvokeLlmAsync(InvocationContext context);
        Task<ChatHistory> ContinueConversationAsync(ChatHistory chatHistory, ConversationContinuation details);
        
        // New streaming methods
        IAsyncEnumerable<string> StreamingInvokeLlmAsync(
            InvocationContext context, 
            CancellationToken cancellationToken = default);
            
        IAsyncEnumerable<string> StreamingContinueConversationAsync(
            ChatHistory chatHistory, 
            ConversationContinuation details,
            CancellationToken cancellationToken = default);
    }
}
