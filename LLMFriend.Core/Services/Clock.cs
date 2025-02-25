using System;

namespace LLMFriend
{
    public class Clock : IClock
    {
        public DateTime GetNow() => DateTime.Now;
    }
}
