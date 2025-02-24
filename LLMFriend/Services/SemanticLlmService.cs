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
                var pipelineInput = prompt;

                // Add tool capabilities
                _kernel.ImportSkill(_llmToolService, "tools");

                // Execute the pipeline with timeout
                var response = await _kernel.RunAsync(
                    pipelineInput,
                    cancellation: _cts.Token,
                    timeout: _config.TimeForExpectedReplyInConversation
                );

                // If the response indicates timeout, send a special message to the LLM
                if (response.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("LLM did not respond within the expected timeframe.");
                    await _kernel.RunAsync("Please respond as there was a timeout in the previous interaction.", cancellation: _cts.Token);
                }
                else
                {
                    _logger.LogInformation("LLM Response: {Response}", response);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("LLM invocation was canceled due to timeout.");
                await _kernel.RunAsync("Operation timed out. Please continue the conversation.", cancellation: _cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during LLM invocation: {ErrorMessage}", ex.Message);
            }
        }
    }
}
