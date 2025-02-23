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
    public class ChatService : IHostedService
    {
        private readonly ILlmToolService _llmToolService;
        private readonly ISchedulingService _schedulingService;
        private readonly IOptionsMonitor<ConfigurationModel> _configMonitor;
        private readonly ILogger<ChatService> _logger;
        private CancellationTokenSource _cts;

        public ChatService(
            ILlmToolService llmToolService,
            ISchedulingService schedulingService,
            IOptionsMonitor<ConfigurationModel> configMonitor,
            ILogger<ChatService> logger)
        {
            _llmToolService = llmToolService;
            _schedulingService = schedulingService;
            _configMonitor = configMonitor;
            _logger = logger;
            _cts = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() => RunChatAsync(_cts.Token), cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }

        private async Task RunChatAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Terminal chat interface started. Type /help for a list of commands.");

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write("> ");
                string? input = await Task.Run(() => Console.ReadLine(), cancellationToken);

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                if (!input.StartsWith("/"))
                {
                    Console.WriteLine("Invalid command format. Commands should start with '/'. Type /help for assistance.");
                    continue;
                }

                var args = input.Split(' ', 2);
                var command = args[0].ToLower();
                var argument = args.Length > 1 ? args[1] : string.Empty;

                switch (command)
                {
                    case "/set":
                        if (string.IsNullOrWhiteSpace(argument))
                        {
                            Console.WriteLine("Usage: /set <memory>");
                        }
                        else
                        {
                            _llmToolService.StoreMemory(argument);
                            Console.WriteLine($"Memory stored: {argument}");
                        }
                        break;

                    case "/help":
                        ShowHelp();
                        break;

                    case "/schedule":
                        ShowSchedule();
                        break;

                    case "/pause":
                        ToggleAutonomousFeatures();
                        break;

                    default:
                        Console.WriteLine("Unknown command. Type /help for a list of available commands.");
                        break;
                }
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("/set <memory> - Stores the provided string as a memory.");
            Console.WriteLine("/help - Displays this help message.");
            Console.WriteLine("/schedule - Shows the next scheduled invocation time and autonomous features status.");
            Console.WriteLine("/pause - Toggles autonomous features on or off.");
            Console.WriteLine($"Configuration file location: {AppContext.BaseDirectory}appsettings.json");
            Console.WriteLine($"Memory bank folder: {_configMonitor.CurrentValue.MemoryBankFolder}");
        }

        private void ShowSchedule()
        {
            var nextInvocation = _schedulingService.GetNextInvocationTime();
            string status = _configMonitor.CurrentValue.AutonomousFeaturesEnabled ? "Enabled" : "Paused";

            Console.WriteLine($"Next Invocation Time: {nextInvocation}");
            Console.WriteLine($"Autonomous Features: {status}");
        }

        private void ToggleAutonomousFeatures()
        {
            var config = _configMonitor.CurrentValue;
            config.AutonomousFeaturesEnabled = !config.AutonomousFeaturesEnabled;
            Console.WriteLine($"Autonomous Features are now {(config.AutonomousFeaturesEnabled ? "Enabled" : "Paused")}.");
            // Note: To update the configuration dynamically, additional implementation is required.
            // TODO: just write over our config's JSON file.
        }
    }
}
