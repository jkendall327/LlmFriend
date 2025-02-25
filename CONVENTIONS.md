Use proper structured logging. For example: logger.LogInformation("This is a log message {LoggedVariable}", variableValue);
Log messages should not end in a trailing period.
Prefer file-scoped namespaces.
Prefer primary constructors.
Prefer 'var'.
Avoid excessive indentation.

When writing tests:
Use NSubstitute and Xunit.
Don't bother with pointless // Arrange or // Assert comments.
