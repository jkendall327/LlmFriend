using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LLMFriend.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LLMFriend.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LLMFriend.Services
{
    public class ChatService
    {
        private readonly ILlmToolService _llmToolService;
        private readonly TimeProvider _clock;
        private readonly ILlmService _llmService;
        private readonly IOptionsMonitor<ConfigurationModel> _configMonitor;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            ILlmToolService llmToolService,
            IOptionsMonitor<ConfigurationModel> configMonitor,
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

        private ChatHistory? _history;

        public async Task<string> InitiateChatAsync(string? userMessage, CancellationToken cancellationToken = default)
        {
            var type = InvocationType.Autonomous;
            
            if (!string.IsNullOrWhiteSpace(userMessage))
            {
                type = InvocationType.UserInitiated;
            }
            
            var invocationContext = new InvocationContext
            {
                InvocationTime = _clock.GetLocalNow(),
                Type = type,
                Username = Environment.UserName,
                FileList = _llmToolService.ReadEnvironment().ToArray(),
                UserStartingMessage = userMessage
            };

            _history = await _llmService.InvokeLlmAsync(invocationContext);
            return _history.Last().Content;
        }

        public async Task<string> ContinueChatAsync(string userMessage, CancellationToken cancellationToken = default)
        {
            if (_history == null)
            {
                throw new InvalidOperationException("Chat must be initiated before continuing");
            }

            var stopwatch = Stopwatch.StartNew();
            _history.AddUserMessage(userMessage);

            var continuation = new ConversationContinuation(stopwatch.Elapsed, false);
            _history = await _llmService.ContinueConversationAsync(_history, continuation);
            
            return _history.Last().Content;
        }

        public async IAsyncEnumerable<string> GetStreamingResponseAsync(string userMessage, bool isInitial = false)
        {
            // Simulate streaming for now
            var response = isInitial 
                ? await InitiateChatAsync(userMessage)
                : await ContinueChatAsync(userMessage);

            foreach (var word in response.Split(' '))
            {
                await Task.Delay(100);
                yield return word + " ";
            }
        }
    }
}
