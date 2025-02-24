using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using LLMFriend.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LLMFriend.Configuration;

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
                await _llmService.InvokeLlmAsync(invocationContext);

                var inputTask = Task.Run(Console.ReadLine, cancellationToken);
                var timeoutTask = Task.Delay(_configMonitor.CurrentValue.TimeForExpectedReplyInConversation, cancellationToken);

                var completedTask = await Task.WhenAny(inputTask, timeoutTask);
    
                if (completedTask == inputTask)
                {
                    // User responded in time.
                    var userInput = inputTask.Result;
                }
                else
                {
                    // Timeout reached before user input.
                    Console.WriteLine("User did not respond in time.");
                }
    
                await _llmService.InvokeLlmAsync(invocationContext);
            }
        }
    }
}
