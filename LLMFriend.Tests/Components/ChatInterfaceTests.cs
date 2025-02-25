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
using NSubstitute;
using Xunit;

namespace LLMFriend.Tests.Components;

public class ChatInterfaceTests : TestContext
{
    private readonly IChatService _chatService;
    private readonly IJSRuntime _jsRuntime;
    
    public ChatInterfaceTests()
    {
        _chatService = Substitute.For<IChatService>();
        _jsRuntime = Substitute.For<IJSRuntime>();
        
        Services.AddSingleton(_chatService);
        Services.AddSingleton(_jsRuntime);
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
        _chatService
            .GetStreamingResponseAsync(
                Arg.Any<Guid>(), 
                Arg.Any<string>(), 
                Arg.Any<bool>(), 
                Arg.Any<CancellationToken>())
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
        _chatService
            .GetStreamingResponseAsync(
                Arg.Any<Guid>(), 
                Arg.Any<string>(), 
                Arg.Any<bool>(), 
                Arg.Any<CancellationToken>())
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
