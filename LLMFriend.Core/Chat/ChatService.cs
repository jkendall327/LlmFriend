using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LLMFriend.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LLMFriend.Services;

public interface IChatService
{
    void RemoveChat(Guid chatId);

    IAsyncEnumerable<string> GetStreamingResponseAsync(
        Guid chatId,
        string userMessage, 
        bool isInitial = false,
        CancellationToken cancellationToken = default);
}

public class ChatService : IChatService
{
    private readonly ILlmToolService _llmToolService;
    private readonly TimeProvider _clock;
    private readonly ILlmService _llmService;
    private readonly IOptionsMonitor<AppConfiguration> _configMonitor;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        ILlmToolService llmToolService,
        IOptionsMonitor<AppConfiguration> configMonitor,
        ILogger<ChatService> logger,
        ILlmService llmService,
        TimeProvider clock)
    {
        _llmToolService = llmToolService;
        _configMonitor = configMonitor;
        _logger = logger;
        _llmService = llmService;
        _clock = clock;
    }

    private readonly Dictionary<Guid, ChatHistory> _chatHistories = new();

    private async Task<(string Response, bool TimedOut)> InitiateChatAsync(
        Guid chatId, 
        string? userMessage, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating new chat {ChatId} with message type {MessageType}", 
            chatId, 
            string.IsNullOrWhiteSpace(userMessage) ? "Autonomous" : "UserInitiated");
            
        var type = string.IsNullOrWhiteSpace(userMessage) 
            ? InvocationType.Autonomous 
            : InvocationType.UserInitiated;
            
        var invocationContext = new InvocationContext
        {
            InvocationTime = _clock.GetLocalNow(),
            Type = type,
            Username = Environment.UserName,
            FileList = _llmToolService.ReadEnvironment().ToArray(),
            UserStartingMessage = userMessage
        };

        var history = await _llmService.InvokeLlmAsync(invocationContext);
        _chatHistories[chatId] = history;
        
        _logger.LogInformation("Chat {ChatId} initiated successfully with {MessageCount} messages", 
            chatId, 
            history.Count);
            
        return (history.Last().Content, false);
    }

    private async Task<(string Response, bool TimedOut)> ContinueChatAsync(
        Guid chatId,
        string userMessage, 
        CancellationToken cancellationToken = default)
    {
        if (!_chatHistories.TryGetValue(chatId, out var history))
        {
            _logger.LogError("Attempted to continue non-existent chat {ChatId}", chatId);
            throw new InvalidOperationException($"Chat {chatId} not found");
        }

        _logger.LogInformation("Continuing chat {ChatId} with message length {MessageLength}", 
            chatId, 
            userMessage.Length);

        var stopwatch = Stopwatch.StartNew();
        history.AddUserMessage(userMessage);

        var timeoutMs = _configMonitor.CurrentValue.TimeForExpectedReplyInConversation;
        var timeoutTask = Task.Delay(timeoutMs, cancellationToken);
        var responseTask = _llmService.ContinueConversationAsync(
            history, 
            new ConversationContinuation(stopwatch.Elapsed, false));

        var completedTask = await Task.WhenAny(responseTask, timeoutTask);
        var timedOut = completedTask == timeoutTask;

        if (timedOut)
        {
            _logger.LogWarning("Chat {ChatId} response timed out after {ElapsedMs}ms", 
                chatId, 
                stopwatch.ElapsedMilliseconds);
                
            // Let the LLM know about the timeout
            history = await _llmService.ContinueConversationAsync(
                history,
                new ConversationContinuation(stopwatch.Elapsed, true));
        }
        else
        {
            _logger.LogInformation("Chat {ChatId} response received in {ElapsedMs}ms", 
                chatId, 
                stopwatch.ElapsedMilliseconds);
                
            history = await responseTask;
        }

        _chatHistories[chatId] = history;
        return (history.Last().Content, timedOut);
    }

    public void RemoveChat(Guid chatId)
    {
        var removed = _chatHistories.Remove(chatId);
        _logger.LogInformation("Chat {ChatId} removed: {WasRemoved}", chatId, removed);
    }

    public async IAsyncEnumerable<string> GetStreamingResponseAsync(
        Guid chatId,
        string userMessage, 
        bool isInitial = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting {ChatType} streaming response for chat {ChatId}", 
            isInitial ? "initial" : "continuation", 
            chatId);
            
        var (response, timedOut) = isInitial
            ? await InitiateChatAsync(chatId, userMessage, cancellationToken)
            : await ContinueChatAsync(chatId, userMessage, cancellationToken);

        if (timedOut)
        {
            _logger.LogWarning("Streaming response for chat {ChatId} timed out", chatId);
            yield return "[Response timed out] ";
        }

        var wordCount = response.Split(' ').Length;
        _logger.LogInformation("Streaming {WordCount} words for chat {ChatId}", wordCount, chatId);

        foreach (var word in response.Split(' '))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Streaming for chat {ChatId} was cancelled", chatId);
                yield break;
            }
            
            await Task.Delay(50, cancellationToken);
            yield return word + " ";
        }
        
        _logger.LogInformation("Completed streaming response for chat {ChatId}", chatId);
    }
}
