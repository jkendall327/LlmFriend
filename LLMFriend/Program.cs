using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO.Abstractions;
using LLMFriend;
using LLMFriend.Configuration;
using LLMFriend.Services;
using Microsoft.SemanticKernel;

var host = Host.CreateApplicationBuilder(args);

host.Configuration.AddUserSecrets(typeof(Program).Assembly);

var services = host.Services;

services.Configure<ConfigurationModel>(host.Configuration.GetSection("ConfigurationModel"));

services.AddSingleton<IClock, Clock>();
services.AddSingleton<IFileSystem, FileSystem>();
services.AddSingleton<ILlmToolService, LlmToolService>();
services.AddSingleton<ISchedulingService, SchedulingService>();
services.AddSingleton<PersonalityService>();
services.AddSingleton<ILlmService, SemanticLlmService>();
services.AddSingleton<ChatService>();

var apiKey = host.Configuration.GetRequiredSection("ConfigurationModel").GetRequiredSection("DeepseekApiKey").Value;
services.AddSingleton<Kernel>();

#pragma warning disable SKEXP0010
services.AddOpenAIChatCompletion("deepseek-reasoner",
    new Uri("https://api.deepseek.com"),
    apiKey);
#pragma warning restore SKEXP0010

var app = host.Build();

var chat = app.Services.GetRequiredService<ChatService>();

// Pass user's starting message if initiated by them
await chat.RunChatAsync(args.FirstOrDefault());


// a daemon that invokes this app.
// first arg is the invocation type
// second arg is user message?