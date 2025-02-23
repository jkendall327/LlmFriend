using System;
using System.Threading.Tasks;

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
        Task InvokeLlmAsync(InvocationContext context);
    }
}
