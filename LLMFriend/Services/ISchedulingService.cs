using System;

namespace LLMFriend.Services
{
    public interface ISchedulingService
    {
        DateTime GetNextInvocationTime();
    }
}
