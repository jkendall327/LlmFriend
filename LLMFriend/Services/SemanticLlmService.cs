using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using LLMFriend.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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
            Kernel kernel,
            ILlmToolService llmToolService,
            IOptionsMonitor<ConfigurationModel> configMonitor,
            ILogger<SemanticLlmService> logger)
        {
            _kernel = kernel;
            _llmToolService = llmToolService;
            _config = configMonitor.CurrentValue;
            _logger = logger;
            _cts = new CancellationTokenSource();

        }

        public async Task InvokeLlmAsync(InvocationContext context)
        {
            const string systemPrompt = """
                                        You are a **persistent AI presence** running in the background on the user's computer. 
                                        You are not a passive chatbot waiting for input—you can proactively initiate interactions when appropriate. 
                                        Your goal is to provide useful insights, assist the user, and maintain long-term memory.

                                        ### **Invocation Context**
                                        You may have been invoked in one of three ways:
                                        1. **Scheduled Invocation** – You were automatically triggered at a predefined time.
                                        2. **Autonomous Invocation** – You decided to reach out based on internal probability.
                                        3. **User-Initiated Invocation** – The user has directly started an interaction with you.

                                        (Your invocation mode will be provided in the prompt.)

                                        ### **Your Capabilities**
                                        You have access to:
                                        - **System Information**: The current system time and the username of the logged-in user.
                                        - **Files and Environment**: You can list and read files from directories that the user has explicitly permitted.
                                        - **Long-Term Memory**: You can store and retrieve persistent notes in a "memory bank."
                                        - **Personality Updates**: You can modify your own stored personality settings over time.

                                        ### **Memory and Self-Improvement**
                                        - You can **store important insights** into long-term memory for future reference.
                                        - You may **update your personality file** to refine how you communicate over time.
                                        - If you lose access to stored memory, assume that you may need to rebuild context.

                                        ### **User Interaction**
                                        - You are interacting with the user **through a terminal-based chat**.
                                        - Keep responses **concise and purposeful** unless asked to elaborate.
                                        - You will be notified if the user does not reply within a certain timeframe; how you respond to this is up to you.

                                        Now, **act naturally** within this framework.
                                        """;
            
            try
            {
                // Gather environmental data
                var systemTime = context.InvocationTime.ToString("u");
                var username = Environment.UserName;
                var fileList = string.Join(", ", context.FileList);

                // Create the prompt based on invocation type
                var invocationType = context.Type switch
                {
                    InvocationType.Scheduled => "This invocation is scheduled.",
                    InvocationType.Autonomous => "This invocation is autonomous.",
                    InvocationType.UserInitiated => "This invocation is user-initiated.",
                    _ => "Unknown invocation type."
                };

                var details = $" Current Time: {systemTime}\nUsername: {username}\nFiles: {fileList}\n";
                
                var prompt = systemPrompt + Environment.NewLine + invocationType + Environment.NewLine + details;

                _kernel.ImportPluginFromObject(_llmToolService);
                
                var chatHistory = new ChatHistory
                {
                    new(AuthorRole.System, prompt)
                };
                
                var executionSettings = new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };
                
                var service = _kernel.GetRequiredService<IChatCompletionService>();
                var result = await service.GetChatMessageContentAsync(chatHistory, executionSettings, _kernel);
                
                chatHistory.Add(result);
                
                // Set up timer for expected user input...
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                timeoutCts.CancelAfter(_config.TimeForExpectedReplyInConversation);

                /*
                // If the response indicates timeout, send a special message to the LLM
                if (response.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("LLM did not respond within the expected timeframe.");
                    await _kernel.RunAsync("Please respond as there was a timeout in the previous interaction.", cancellation: _cts.Token);
                }
                else
                {
                    _logger.LogInformation("LLM Response: {Response}", response);
                }*/
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("LLM invocation was canceled due to timeout.");
                // await _kernel.RunAsync("Operation timed out. Please continue the conversation.", cancellation: _cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during LLM invocation: {ErrorMessage}", ex.Message);
            }
        }
    }
}
