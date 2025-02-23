using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using LLMFriend.Configuration;

namespace LLMFriend.Services
{
    public class SemanticLlmService : ILlmService
    {
        private readonly ILlmToolService _llmToolService;
        private readonly ConfigurationModel _config;
        private readonly ILogger<SemanticLlmService> _logger;
        private readonly Kernel _kernel;
        private readonly CancellationTokenSource _cts;

        public SemanticLlmService(
            ILlmToolService llmToolService,
            IOptionsMonitor<ConfigurationModel> configMonitor,
            ILogger<SemanticLlmService> logger)
        {
            _llmToolService = llmToolService;
            _config = configMonitor.CurrentValue;
            _logger = logger;
            _cts = new CancellationTokenSource();

            _kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion("davinci", _config.OpenAIApiKey)
                .Build();
        }

        public async Task InvokeLlmAsync(InvocationContext context)
        {
            try
            {
                // Gather environmental data
                var systemTime = context.InvocationTime.ToString("u");
                var username = Environment.UserName;
                var fileList = string.Join(", ", context.FileList);

                // Create the prompt based on invocation type
                string prompt = context.Type switch
                {
                    InvocationType.Scheduled => "This invocation is scheduled.",
                    InvocationType.Autonomous => "This invocation is autonomous.",
                    InvocationType.UserInitiated => "This invocation is user-initiated.",
                    _ => "Unknown invocation type."
                };

                prompt += $" Current Time: {systemTime}\nUsername: {username}\nFiles: {fileList}\n";

                // Define the pipeline
                var pipeline = _kernel.CreateFunctionFromPrompt($"{prompt}\n{_config.PersonalityProfilePath}");

                // Add tool capabilities
                //_kernel.ImportSkill(_llmToolService, "tools");

                // Execute the pipeline with timeout
                // var response = await _kernel.RunAsync(
                //     pipeline,
                //     cancellation: _cts.Token,
                //     timeout: _config.TimeForExpectedReplyInConversation
                // );

                // if (response.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                // {
                //     // Handle timeout by sending a special message back to the LLM
                //     _logger.LogWarning("LLM did not respond within the expected timeframe.");
                //     //await _kernel.RunAsync("Please respond as there was a timeout in the previous interaction.", cancellation: _cts.Token);
                // }
                // else
                // {
                //     _logger.LogInformation("LLM Response: {Response}", response);
                // }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("LLM invocation was canceled due to timeout.");
                // Optionally, send a special message back to the LLM
                //await _kernel.RunAsync("Operation timed out. Please continue the conversation.", cancellation: _cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during LLM invocation: {ErrorMessage}", ex.Message);
            }
        }
    }

    // Simple logger adapter for Semantic Kernel
    public class SemanticKernelLogger
    {
        private readonly ILogger _logger;

        public SemanticKernelLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void LogError(string message)
        {
            _logger.LogError(message);
        }

        public void LogError(string message, Exception ex)
        {
            _logger.LogError(ex, message);
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }
    }
}
