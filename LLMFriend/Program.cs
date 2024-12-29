using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using sk_customllm;

var phi2 = new CustomChatCompletionService
{
    ModelUrl = "http://localhost:1234/v1/chat/completions"
};

var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton<IChatCompletionService>(phi2);
var kernel = builder.Build();

var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory();
history.AddSystemMessage("You are a useful assistant that replies using a funny style and emojis.");
history.AddUserMessage("hi, who are you?");

var result = await chat.GetChatMessageContentsAsync(history);