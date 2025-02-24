using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LLMFriend.Services
{
    public enum InvocationType
    {
        Scheduled,
        Autonomous,
        UserInitiated
    }

    public class InvocationContext
    {
        public DateTime InvocationTime { get; set; }
        public InvocationType Type { get; set; }
        public string Username { get; set; }
        public string[] FileList { get; set; }
    }

    public interface ILlmService
    {
        Task<ChatHistory> InvokeLlmAsync(InvocationContext context);
        Task<ChatHistory> ContinueConversationAsync(ChatHistory chatHistory, ConversationContinuation details);
    }
}
