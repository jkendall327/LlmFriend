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
            
        return (history.Last().Content, false);
    }

    private async Task<(string Response, bool TimedOut)> ContinueChatAsync(
        Guid chatId,
        string userMessage, 
        CancellationToken cancellationToken = default)
    {
        if (!_chatHistories.TryGetValue(chatId, out var history))
        {
            throw new InvalidOperationException($"Chat {chatId} not found");
        }

        var stopwatch = Stopwatch.StartNew();
        history.AddUserMessage(userMessage);

        var timeoutTask = Task.Delay(_configMonitor.CurrentValue.TimeForExpectedReplyInConversation, cancellationToken);
        var responseTask = _llmService.ContinueConversationAsync(
            history, 
            new ConversationContinuation(stopwatch.Elapsed, false));

        var completedTask = await Task.WhenAny(responseTask, timeoutTask);
        var timedOut = completedTask == timeoutTask;

        if (timedOut)
        {
            // Let the LLM know about the timeout
            history = await _llmService.ContinueConversationAsync(
                history,
                new ConversationContinuation(stopwatch.Elapsed, true));
        }
        else
        {
            history = await responseTask;
        }

        _chatHistories[chatId] = history;
        return (history.Last().Content, timedOut);
    }

    public void RemoveChat(Guid chatId)
    {
        _chatHistories.Remove(chatId);
    }

    public async IAsyncEnumerable<string> GetStreamingResponseAsync(
        Guid chatId,
        string userMessage, 
        bool isInitial = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (response, timedOut) = isInitial
            ? await InitiateChatAsync(chatId, userMessage, cancellationToken)
            : await ContinueChatAsync(chatId, userMessage, cancellationToken);

        if (timedOut)
        {
            yield return "[Response timed out] ";
        }

        foreach (var word in response.Split(' '))
        {
            if (cancellationToken.IsCancellationRequested) yield break;
            await Task.Delay(50, cancellationToken);
            yield return word + " ";
        }
    }
}