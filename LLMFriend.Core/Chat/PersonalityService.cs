using LLMFriend.Configuration;
using Microsoft.Extensions.Options;

namespace LLMFriend.Services;

public class PersonalityService
{
    private readonly IOptionsMonitor<AppConfiguration> _options;

    public PersonalityService(IOptionsMonitor<AppConfiguration> options)
    {
        _options = options;
    }

    public async Task<string> GetPersonalityAsync()
    {
        var path = _options.CurrentValue.PersonalityProfilePath;

        if (!File.Exists(path))
        {
            throw new InvalidOperationException("No personality file found");
        }
        
        return await File.ReadAllTextAsync(path);
    }
}