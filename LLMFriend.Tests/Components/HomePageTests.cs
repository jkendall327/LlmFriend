using Bunit;
using LLMFriend.Web.Components.Chat;
using LLMFriend.Web.Components.Pages;
using LLMFriend.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Threading.Channels;

namespace LLMFriend.Tests.Components;

public class HomePageTests : TestContext
{
    private readonly ChatNotificationService _notificationService = Substitute.For<ChatNotificationService>();
    private readonly Channel<DateTimeOffset> _testChannel;

    public HomePageTests()
    {
        _testChannel = Channel.CreateUnbounded<DateTimeOffset>();
        _notificationService.GetReader().Returns(_testChannel.Reader);
        Services.AddSingleton(_notificationService);
        
        // Register the ChatInterface component for rendering
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(Substitute.For<IServiceProvider>());
    }

    [Fact]
    public void Home_ShouldRender_WithStartNewChatButton_WhenEmpty()
    {
        var cut = RenderComponent<Home>();

        var button = cut.Find(".new-chat-button");
        Assert.Equal("Start New Chat", button.TextContent);
        Assert.Empty(cut.FindAll("div.chat-interface"));
    }

    [Fact]
    public void CreateNewChat_ShouldAddChatInterface_WhenClicked()
    {
        var cut = RenderComponent<Home>();

        // Click the new chat button
        cut.Find(".new-chat-button").Click();

        // Verify a chat interface was added
        var chatInterfaces = cut.FindComponents<ChatInterface>();
        Assert.Single(chatInterfaces);
        
        // Button text should change after first chat is created
        var button = cut.Find(".new-chat-button");
        Assert.Equal("New Chat", button.TextContent);
    }

    [Fact]
    public void CloseChat_ShouldRemoveChatInterface_WhenTriggered()
    {
        var cut = RenderComponent<Home>();

        // Add a chat
        cut.Find(".new-chat-button").Click();
        
        // Find the close button on the chat interface and click it
        var closeButton = cut.Find(".close-chat-button");
        closeButton.Click();

        // Verify the chat was removed
        Assert.Empty(cut.FindComponents<ChatInterface>());
    }

    [Fact]
    public async Task Notification_ShouldCreateNewChat_WhenReceived()
    {
        var cut = RenderComponent<Home>();
        
        // Initially no chats
        Assert.Empty(cut.FindComponents<ChatInterface>());
        
        // Send a notification
        await _testChannel.Writer.WriteAsync(DateTimeOffset.Now);
        
        // Wait for the component to process the notification
        cut.WaitForState(() => cut.FindComponents<ChatInterface>().Count > 0);
        
        // Verify a new chat was created
        Assert.Single(cut.FindComponents<ChatInterface>());
    }

    [Fact]
    public void MultipleChats_ShouldBeManaged_Correctly()
    {
        var cut = RenderComponent<Home>();
        
        // Add three chats
        cut.Find(".new-chat-button").Click();
        cut.Find(".new-chat-button").Click();
        cut.Find(".new-chat-button").Click();
        
        // Verify three chats exist
        var chatInterfaces = cut.FindComponents<ChatInterface>();
        Assert.Equal(3, chatInterfaces.Count);
        
        // Close the second chat (index 1)
        cut.FindAll(".close-chat-button")[1].Click();
        
        // Verify two chats remain
        chatInterfaces = cut.FindComponents<ChatInterface>();
        Assert.Equal(2, chatInterfaces.Count);
    }
}
