1. Overview

This application is a cross-platform (.NET 9) background service designed to provide a novel interaction model with large language models (LLMs). By granting the LLM a degree of agency, it acts as a persistent, context-aware presence on the user’s PC. The app interacts with its environment, a configuration file, and a memory bank, and communicates with the user via a terminal-based chat interface.
2. Functional Requirements
2.1. Core Functionality

    Persistent Background Service:
        The app runs continuously in the background.
        It is cross-platform with a focus on Linux while still supporting Windows.

    LLM Invocation:
        The LLM is invoked daily at a configurable time using a crontab-like scheduling mechanism.
        The LLM can also autonomously initiate a conversation based on a configurable probability.
        The LLM expects a reply from the user within a configured timeframe; if not received, the app notifies the LLM, prompting it to reinitiate or adjust its conversation.

    Environment Data Collection:
        The LLM reads from:
            The current system time.
            The current username.
            The text content of files from user-specified allowed file paths.
            A memory bank folder for persistent long-term storage.

    Memory Management:
        The LLM can store memories by invoking a designated tool.
        Memories can be stored as plain text or JSON (if metadata such as timestamps is needed).

    Semantic Kernel Integration:
        Use Microsoft Semantic Kernel to abstract the LLM provider.
        The kernel should expose a standardized interface that makes it easy to swap between different LLM implementations without rearchitecting the app.

3. Configuration
3.1. JSON Config File

The JSON configuration file should include at least the following fields:

    AllowedFilePathsForSearch:
        Type: string[]
        Description: A list of file paths that the app is allowed to access for content reading.

    CrontabForScheduledInvocation:
        Type: string
        Description: A crontab expression defining when the daily LLM invocation should occur.

    ProbabilityOfStartingConversationsAutonomously:
        Type: double
        Description: A probability (0.0 to 1.0) that governs whether the LLM will autonomously initiate a conversation outside of the scheduled time.

    TimeForExpectedReplyInConversation:
        Type: TimeSpan
        Description: The amount of time the LLM should wait for a user reply before being informed of the timeout.

Additionally, the configuration will include a global flag (managed via IOptionsMonitor<T>) that can pause or resume autonomous features (affecting both the crontab-based invocation and autonomous conversations).
3.2. Dynamic Reloading

    Utilize IOptionsMonitor<T> to support dynamic reloading of the configuration file at runtime. Changes to the config (e.g., file paths, scheduling, probability values) should take effect without requiring a restart.

4. Tools via Semantic Kernel

The LLM will interact with the environment by invoking the following tools through the semantic kernel abstraction:

    ReadEnvironment()
        Signature: List<Filepath> ReadEnvironment()
        Function: Returns a list of file paths that are available based on the allowed paths from the config.
        Error Handling: If the file system cannot be accessed, return an empty list.

    ReadFile()
        Signature: string ReadFile(Filepath filepath)
        Function: Reads and returns the content of the specified file.
        Error Handling: If a file cannot be read (permissions, missing file, etc.), return an empty string.

    StoreMemory()
        Signature: void StoreMemory(string memory)
        Function: Stores the provided memory string to the memory bank folder. Optionally, this could be extended to JSON format to include metadata (e.g., timestamp).
        Error Handling: Failures in writing are silently ignored (the app logs the error via ILogger<T> but does not notify the LLM).

    UpdatePersonality()
        Signature: void UpdatePersonality(string newPersonality)
        Function: Updates the personality profile file that defines the LLM's character and tone.
        Error Handling: Similar to the other tools, any file access issues are caught by the app layer and handled silently (with logging).

5. Terminal Chat Interface
5.1. User Interaction

    Interface:
        A simple terminal window will serve as the chat interface.

    Conversation Flow:
        The conversation is minimal, without advanced prompt logic.
        The LLM receives context in its prompt indicating the source of invocation:
            Scheduled (crontab)
            Autonomous
            User-initiated

    Profile File:
        A personality profile file is sent to the LLM at startup to set its character and tone.

5.2. Manual Commands

The terminal interface will support the following commands:

    /set:
        Usage: /set <arbitrary string>
        Function: Stores the provided string as a memory (using the same mechanism as the LLM would).

    /help:
        Usage: /help
        Function: Displays a list of all available commands with brief descriptions, as well as details regarding the configuration file location and memory bank folder.

    /schedule:
        Usage: /schedule
        Function: Displays the next scheduled invocation time and indicates whether autonomous conversations are enabled or currently paused.

    /pause:
        Usage: /pause
        Function: Toggles a global flag in the configuration that disables/enables both the crontab-based scheduled invocations and autonomous conversation initiations.
        Toggle Behavior: Repeating the command will re-enable the features.

    Parsing:
        Use a standard NuGet command-parsing library to handle these commands, ensuring robust parsing and error messaging for invalid commands.

6. Scheduling and Time Management

    Scheduling:
        Daily invocation of the LLM is controlled by a crontab expression provided in the config.
        The app must compute the next scheduled time and trigger the invocation accordingly.

    Timeout Management:
        If no reply is received within the TimeForExpectedReplyInConversation, the app will send a special message to the LLM informing it of the timeout.

    Time Abstraction:
        Use an abstraction layer for system time (e.g., via an injected IClock service) to enable easier unit testing without relying on the real system clock.

7. Logging & Error Handling

    Logging:
        Use ILogger<T> for logging events such as:
            Scheduled invocations and autonomous triggers.
            Tool usage events (e.g., when files are read, memories stored, etc.).
            Errors (e.g., filesystem issues, scheduling errors) for debugging and diagnostics.
        The logger should be configurable so that additional logging sinks can be easily added.

    Error Handling Strategy:
        The application layer should handle errors in file access and scheduling silently from the perspective of the LLM.
        The LLM is never directly exposed to errors; instead, the app logs errors and provides fallback behavior (e.g., returning an empty list for file reads).

8. Testing Strategy
8.1. Unit Testing

    Tool Methods:
        Test each of the four main tool functions:
            Validate that ReadEnvironment() correctly filters files based on allowed paths.
            Ensure ReadFile() returns file contents (and handles error cases by returning an empty string).
            Confirm StoreMemory() writes to the correct memory bank folder.
            Verify UpdatePersonality() updates the personality file as expected.
    Scheduling:
        Write unit tests for the scheduling logic, including correct interpretation of the crontab expression and proper calculation of the next invocation time.
    Command Parsing:
        Test the terminal command parser to ensure that /set, /help, /schedule, and /pause commands are parsed correctly and that invalid commands result in appropriate error messages.

8.2. Integration Testing

    Time Abstraction:
        Use dependency injection to substitute the real system clock with a fake clock for testing scheduled invocations.

    Filesystem Abstraction:
        Employ System.IO.Abstractions to create mock filesystems during tests so that tests are not dependent on the real filesystem.

    Semantic Kernel Integration:
        While the semantic kernel itself does not require extensive testing, ensure that your abstraction layer (wrapping the LLM provider) behaves consistently when swapping different LLM backends.

8.3. General Test Considerations

    Focus on testing the tool-use functions and core scheduling/command functionalities.
    Avoid testing external dependencies like the actual LLM responses; instead, mock the semantic kernel calls where necessary.

9. Deployment Considerations

    Cross-Platform Compatibility:
        The app should be deployable as a Linux daemon and a Windows service.
        Design the service to be agnostic of the underlying OS where possible.

    Dynamic Configuration Reloading:
        The configuration file should be monitored via IOptionsMonitor<T> so that updates are applied in real time without needing to restart the application.

    Logging and Diagnostics:
        Ensure that logging sinks are configurable so that in production the logs can be routed to files, consoles, or external logging services.

10. Summary of Architectural Choices

    Core Design:
        A persistent background service that interacts with both the environment and the user via a terminal.
        The LLM is given agency via a semantic kernel abstraction, with clearly defined tools for environment interaction and memory management.

    Configuration and Scheduling:
        A JSON config file governs allowed file paths, scheduling (via a crontab expression), conversation timing, and autonomous behavior probability.
        Dynamic reloading of the config is enabled via IOptionsMonitor<T>.

    User Interface & Commands:
        A minimal terminal-based chat interface provides a straightforward user interaction.
        Manual commands (/set, /help, /schedule, /pause) allow for direct control over memory storage and autonomous features.

    Error Handling & Logging:
        The app layer is responsible for handling and logging errors, ensuring the LLM remains unaware of any underlying issues.
        Standardized logging is implemented via ILogger<T>.

    Testability:
        Use abstractions for system time and filesystem access to allow robust unit and integration testing.
        Focus tests on the tool functions, scheduling logic, and command parsing.
