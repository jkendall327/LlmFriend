namespace LLMFriend.Services
{
    public interface ISchedulingService
    {
        DateTimeOffset GetNextInvocationTime();
    }
}
