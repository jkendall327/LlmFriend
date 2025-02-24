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
        
        public async Task RunChatAsync(CancellationToken cancellationToken = default)
        {
            var invocationContext = new InvocationContext
            {
                InvocationTime = _clock.GetNow(),
                Type = InvocationType.Scheduled,
                Username = Environment.UserName,
                FileList = _llmToolService.ReadEnvironment().ToArray()
            };

            while (true)
            {
                _history = await _llmService.InvokeLlmAsync(invocationContext);

                var stopwatch = Stopwatch.StartNew();
                var inputTask = Task.Run(Console.ReadLine, cancellationToken);
                var timeoutTask = Task.Delay(_configMonitor.CurrentValue.TimeForExpectedReplyInConversation, cancellationToken);

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
            }
        }
    }
}
