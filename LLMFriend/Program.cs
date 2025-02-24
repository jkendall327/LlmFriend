using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.IO.Abstractions;
using System.Threading.Tasks;
using LLMFriend;
using LLMFriend.Configuration;
using LLMFriend.Services;
using Cronos;
using Microsoft.SemanticKernel;

var host = Host.CreateApplicationBuilder(args);

host.Configuration.AddUserSecrets(typeof(Program).Assembly);

var services = host.Services;

services.Configure<ConfigurationModel>(host.Configuration.GetSection("ConfigurationModel"));

services.AddSingleton<IClock, Clock>();
services.AddSingleton<IFileSystem, FileSystem>();
services.AddSingleton<ILlmToolService, LlmToolService>();
services.AddSingleton<ISchedulingService, SchedulingService>();
services.AddSingleton<ILlmService, SemanticLlmService>();
services.AddHostedService<ChatService>();

var apiKey = host.Configuration.GetRequiredSection("ConfigurationModel").GetRequiredSection("OpenAIApiKey").Value;
services.AddOpenAIChatCompletion("davinci", apiKey);
services.AddSingleton<Kernel>();

// Build the app
var app = host.Build();

var clock = app.Services.GetRequiredService<IClock>();
var llmService = app.Services.GetRequiredService<ILlmService>();
var tools = app.Services.GetRequiredService<ILlmToolService>();

var invocationContext = new InvocationContext
{
    InvocationTime = clock.GetNow(),
    Type = InvocationType.Scheduled,
    Username = Environment.UserName,
    FileList = tools.ReadEnvironment().ToArray()
};

await llmService.InvokeLlmAsync(invocationContext);

await app.RunAsync();
