using LLMFriend.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LLMFriend.Services
{
    public record ConversationContinuation(TimeSpan TimeForUserMessage, bool MessageTookTooLong);
    
    public class SemanticLlmService : ILlmService
    {
        private readonly SystemPromptBuilder _systemPromptBuilder;
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chat;
        private readonly ILogger<SemanticLlmService> _logger;

        private readonly OpenAIPromptExecutionSettings _promptSettings;

        public SemanticLlmService(Kernel kernel,
            ILlmToolService llmToolService,
            ILogger<SemanticLlmService> logger,
            PersonalityService personalityService,
            IOptions<AiModelOptions> modelOptions,
            SystemPromptBuilder systemPromptBuilder)
        {
            _kernel = kernel;
            _logger = logger;
            _systemPromptBuilder = systemPromptBuilder;
            _chat = _kernel.GetRequiredService<IChatCompletionService>();
            
            _kernel.ImportPluginFromObject(llmToolService);
            
            var choice = modelOptions.Value.SupportsToolUse
                ? FunctionChoiceBehavior.Auto()
                : FunctionChoiceBehavior.None();

            _promptSettings = new()
            {
                FunctionChoiceBehavior = choice
            };
        }


        public async Task<ChatHistory> InvokeLlmAsync(InvocationContext context)
        {
            try
            {
                return await InvokeCore(context);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during LLM invocation: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        public async Task<ChatHistory> ContinueConversationAsync(ChatHistory chatHistory, ConversationContinuation details)
        {
            if (details.MessageTookTooLong)
            {
                chatHistory.AddUserMessage($"[SYSTEM]: The user took too long to send their last message ({details.TimeForUserMessage}).");
            }
            
            try
            {
                var result = await _chat.GetChatMessageContentAsync(chatHistory, _promptSettings, _kernel);

                chatHistory.Add(result);
                return chatHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during LLM invocation: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        private async Task<ChatHistory> InvokeCore(InvocationContext context)
        {
            var systemPrompt = await _systemPromptBuilder.BuildSystemPrompt(context);

            var chatHistory = new ChatHistory
            {
                systemPrompt
            };

            if (context.Type is InvocationType.UserInitiated && !string.IsNullOrWhiteSpace(context.UserStartingMessage))
            {
                chatHistory.AddUserMessage(context.UserStartingMessage);
            }
            else
            {
                // It is amusing that this is a valid way to fix bugs when working with AI.
                var content = "[This is a dummy user message that's required for autonomous conversations to start properly. Ignore this message.]";
                chatHistory.AddUserMessage(content);
            }

            var result = await _chat.GetChatMessageContentAsync(chatHistory, _promptSettings, _kernel);

            chatHistory.Add(result);

            return chatHistory;
        }

    }
}