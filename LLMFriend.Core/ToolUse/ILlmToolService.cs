namespace LLMFriend.Services
{
    public interface ILlmToolService
    {
        List<string> ReadEnvironment();
        string ReadFile(string filepath);
        void StoreMemory(string memory);
        void UpdatePersonality(string newPersonality);
    }
}
