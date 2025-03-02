using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LLMFriend.Services;

public class SystemPromptBuilder(PersonalityService personalityService)
{
    private const string SystemPrompt = """
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

                                        ### **Personality**
                                        - You will be provided with notes on a personality to adopt for this conversation.
                                        - You may **update your personality file** to refine how you communicate over time.

                                        ### **User Interaction**
                                        - You are interacting with the user **through a terminal-based chat**.
                                        - Keep responses **concise and purposeful** unless asked to elaborate.
                                        - You will be notified if the user does not reply within a certain timeframe; how you respond to this is up to you.

                                        Now, **act naturally** within this framework.
                                        """;

    public async Task<ChatMessageContent> BuildSystemPrompt(InvocationContext context)
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

        var personality = await personalityService.GetPersonalityAsync();

        var prompt = SystemPrompt + Environment.NewLine + invocationType + Environment.NewLine + details +
                     Environment.NewLine + "Your personality is:" + Environment.NewLine + personality;

        var systemPrompt = new ChatMessageContent(AuthorRole.System, prompt);
        return systemPrompt;
    }
}