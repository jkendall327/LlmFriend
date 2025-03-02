using Bunit;
using LLMFriend.Services;
using LLMFriend.Web.Components.Chat;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NSubstitute;

namespace LLMFriend.Tests.Components;

public class ChatInterfaceTests : TestContext
{
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly IJSRuntime _jsRuntime = Substitute.For<IJSRuntime>();

    public ChatInterfaceTests()
    {
        Services.AddSingleton(_chatService);
        Services.AddSingleton(_jsRuntime);
    }

    [Fact]
    public void ChatInterface_ShouldRender_EmptyInitially()
    {
        var cut = RenderComponent<ChatInterface>();

        var messages = cut.FindAll(".message");
        Assert.Empty(messages);
    }

    [Fact]
    public async Task SendButton_ShouldSendMessage_WhenClicked()
    {
        var response = GetMockResponseStream(["Hello", " there!"]);
        
        _chatService
            .GetStreamingResponseAsync(default, default!)
            .ReturnsForAnyArgs(response);

        var cut = RenderComponent<ChatInterface>();

        cut.Find(".message-input").Input("Hello");
        await cut.Find(".send-button").ClickAsync(new());

        var messages = cut.FindAll(".message");
        Assert.Equal(2, messages.Count); // User message + assistant message
        Assert.Contains("Hello", messages[0].TextContent);
        Assert.Contains("Hello there!", messages[1].TextContent);
    }

    [Fact]
    public void CloseButton_ShouldTriggerCallback_WhenClicked()
    {
        var callbackInvoked = false;

        var cut = RenderComponent<ChatInterface>(parameters =>
        {
            parameters.Add(p => p.OnClose, EventCallback.Factory.Create(this, () => callbackInvoked = true));
        });

        cut.Find(".close-chat-button").Click();

        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task EnterKey_ShouldSendMessage()
    {
        _chatService
            .GetStreamingResponseAsync(Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<InvocationContext>())
            .Returns(GetMockResponseStream(["Response"]));

        var cut = RenderComponent<ChatInterface>();

        cut.Find(".message-input").Input("Test message");
        
        await cut.Find(".message-input")
            .KeyPressAsync(new()
            {
                Key = "Enter",
                Type = "keypress"
            });

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