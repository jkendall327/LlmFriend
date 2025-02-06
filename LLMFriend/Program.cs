using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.ChatCompletion;
using LLMFriend;

var host = Host.CreateApplicationBuilder(args);

var customService = new CustomChatCompletionService
{
    ModelUrl = "http://localhost:1234/v1/chat/completions"
};

host.Services.AddSingleton<IChatCompletionService>(customService);

host.Services.AddHostedService<LlmBackgroundService>();

var app = host.Build();

await app.RunAsync();