using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using LLMFriend.Services;
using LLMFriend.Web.Components.Chat;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace LLMFriend.Tests.Components;

public class ChatInterfaceTests : TestContext
{
    private readonly Mock<IChatService> _mockChatService;
    private readonly Mock<IJSRuntime> _mockJsRuntime;
    
    public ChatInterfaceTests()
    {
        _mockChatService = new Mock<IChatService>();
        _mockJsRuntime = new Mock<IJSRuntime>();
        
        Services.AddSingleton(_mockChatService.Object);
        Services.AddSingleton(_mockJsRuntime.Object);
    }
    
    [Fact]
    public void ChatInterface_ShouldRender_EmptyInitially()
    {
        // Arrange & Act
        var cut = RenderComponent<ChatInterface>();
        
        // Assert
        var messages = cut.FindAll(".message");
        Assert.Empty(messages);
    }
    
    [Fact]
    public async Task SendButton_ShouldSendMessage_WhenClicked()
    {
        // Arrange
        _mockChatService
            .Setup(x => x.GetStreamingResponseAsync(
                It.IsAny<Guid>(), 
                It.IsAny<string>(), 
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
            .Returns(GetMockResponseStream(new[] { "Hello", " there!" }));
        
        var cut = RenderComponent<ChatInterface>();
        
        // Act
        cut.Find(".message-input").Input("Hello");
        await cut.Find(".send-button").ClickAsync(new MouseEventArgs());
        
        // Assert
        var messages = cut.FindAll(".message");
        Assert.Equal(2, messages.Count); // User message + assistant message
        Assert.Contains("Hello", messages[0].TextContent);
        Assert.Contains("Hello there!", messages[1].TextContent);
    }
    
    [Fact]
    public void CloseButton_ShouldTriggerCallback_WhenClicked()
    {
        // Arrange
        bool callbackInvoked = false;
        var cut = RenderComponent<ChatInterface>(parameters => 
            parameters.Add(p => p.OnClose, Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => callbackInvoked = true)));
        
        // Act
        cut.Find(".close-chat-button").Click();
        
        // Assert
        Assert.True(callbackInvoked);
    }
    
    [Fact]
    public async Task EnterKey_ShouldSendMessage()
    {
        // Arrange
        _mockChatService
            .Setup(x => x.GetStreamingResponseAsync(
                It.IsAny<Guid>(), 
                It.IsAny<string>(), 
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
            .Returns(GetMockResponseStream(new[] { "Response" }));
        
        var cut = RenderComponent<ChatInterface>();
        
        // Act
        cut.Find(".message-input").Input("Test message");
        await cut.Find(".message-input").KeyPressAsync(new KeyboardEventArgs { Key = "Enter", Type = "keypress" });
        
        // Assert
        var messages = cut.FindAll(".message");
        Assert.Equal(2, messages.Count);
        Assert.Contains("Test message", messages[0].TextContent);
    }
    
    private static async IAsyncEnumerable<string> GetMockResponseStream(IEnumerable<string> responses)
    {
        foreach (var response in responses)
        {
            yield return response;
            await Task.Delay(10);
        }
    }
}
