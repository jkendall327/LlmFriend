using Microsoft.AspNetCore.Mvc.Testing;

namespace LLMFriend.Tests;

public class AppStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task HealthcheckReturnsOk()
    {
        
    }
}