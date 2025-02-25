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
        public DateTimeOffset InvocationTime { get; set; }
        public InvocationType Type { get; set; }
        public required string Username { get; set; }
        public string[] FileList { get; set; } = [];
        public string? UserStartingMessage { get; set; }
    }

    public interface ILlmService
    {
        Task<ChatHistory> InvokeLlmAsync(InvocationContext context);
        Task<ChatHistory> ContinueConversationAsync(ChatHistory chatHistory, ConversationContinuation details);
    }
}
