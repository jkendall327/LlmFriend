using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
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
        private readonly IClock _clock;
        private readonly ILlmService _llmService;
        private readonly IOptionsMonitor<ConfigurationModel> _configMonitor;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            ILlmToolService llmToolService,
            IOptionsMonitor<ConfigurationModel> configMonitor,
            ILogger<ChatService> logger,
            ILlmService llmService,
            IClock clock)
        {
            _llmToolService = llmToolService;
            _configMonitor = configMonitor;
            _logger = logger;
            _llmService = llmService;
            _clock = clock;
        }

        private ChatHistory? _history;
        
        public async Task RunChatAsync(string? userMessage, CancellationToken cancellationToken = default)
        {
            var type = InvocationType.Autonomous;
            
            if (!string.IsNullOrWhiteSpace(userMessage))
            {
                type = InvocationType.UserInitiated;
            }
            
            var invocationContext = new InvocationContext
            {
                InvocationTime = _clock.GetNow(),
                Type = type,
                Username = Environment.UserName,
                FileList = _llmToolService.ReadEnvironment().ToArray(),
                UserStartingMessage = userMessage
            };

            var timeForExpectedReplyInConversation = _configMonitor.CurrentValue.TimeForExpectedReplyInConversation;

            _history = await _llmService.InvokeLlmAsync(invocationContext);
            Console.WriteLine(_history.Last());

            while (true)
            {
                var stopwatch = Stopwatch.StartNew();
                var inputTask = Console.In.ReadLineAsync(cancellationToken).AsTask();
                var timeoutTask = Task.Delay(timeForExpectedReplyInConversation, cancellationToken);
                
                var completedTask = await Task.WhenAny(inputTask, timeoutTask);

                ConversationContinuation continuation;
                
                if (completedTask == inputTask)
                {
                    // User responded in time.
                    var userInput = inputTask.Result;
                    _history.AddUserMessage(userInput);

                    continuation = new(stopwatch.Elapsed, false);
                }
                else
                {
                    continuation = new(stopwatch.Elapsed, true);
                }
    
                _history = await _llmService.ContinueConversationAsync(_history, continuation);
                
                Console.WriteLine(_history.Last());
            }
        }
    }
}
