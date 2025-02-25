using Microsoft.AspNetCore.Mvc.Testing;

namespace LLMFriend.Tests;

public class AppStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AppStartupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthcheckReturnsOk()
    {
        var client = _factory.CreateClient();
        
        var response = await client.GetAsync("/health");
        
        response.EnsureSuccessStatusCode();
    }
}
