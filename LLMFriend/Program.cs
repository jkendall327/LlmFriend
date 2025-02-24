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

var services = host.Services;

// Configure ConfigurationModel with options monitoring
services.Configure<ConfigurationModel>(host.Configuration.GetSection("ConfigurationModel"));

// Register necessary services
services.AddSingleton<IClock, Clock>();
services.AddSingleton<IFileSystem, FileSystem>();
services.AddSingleton<ILlmToolService, LlmToolService>();
services.AddSingleton<ISchedulingService, SchedulingService>();
services.AddSingleton<ILlmService, SemanticLlmService>();
services.AddHostedService<ChatService>();

var view = host.Configuration.GetDebugView();

var apiKey = host.Configuration.GetRequiredSection("ConfigurationModel").GetRequiredSection("OpenAIApiKey").Value;
services.AddOpenAIChatCompletion("davinci", apiKey);
services.AddSingleton<Kernel>();

// Build the app
var app = host.Build();

// Configure application services
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var configMonitor = app.Services.GetRequiredService<IOptionsMonitor<ConfigurationModel>>();
var clock = app.Services.GetRequiredService<IClock>();
var schedulingService = app.Services.GetRequiredService<ISchedulingService>();
var llmService = app.Services.GetRequiredService<ILlmService>();
var _llmToolService = app.Services.GetRequiredService<LlmToolService>();

// Log current time
logger.LogInformation($"Current Time: {clock.GetNow()}");

// Log configuration values
logger.LogInformation("Configuration Values:");
logger.LogInformation($"AllowedFilePathsForSearch: {string.Join(", ", configMonitor.CurrentValue.AllowedFilePathsForSearch)}");
logger.LogInformation($"CrontabForScheduledInvocation: {configMonitor.CurrentValue.CrontabForScheduledInvocation}");
logger.LogInformation($"ProbabilityOfStartingConversationsAutonomously: {configMonitor.CurrentValue.ProbabilityOfStartingConversationsAutonomously}");
logger.LogInformation($"TimeForExpectedReplyInConversation: {configMonitor.CurrentValue.TimeForExpectedReplyInConversation}");
logger.LogInformation($"AutonomousFeaturesEnabled: {configMonitor.CurrentValue.AutonomousFeaturesEnabled}");

// Log next invocation time
logger.LogInformation($"Next Invocation Time: {schedulingService.GetNextInvocationTime()}");

// Example of triggering LLM invocation via scheduling service
// You might want to set up a timed trigger based on SchedulingService in a hosted service
// For demonstration, we'll invoke it immediately
var invocationContext = new InvocationContext
{
    InvocationTime = clock.GetNow(),
    Type = InvocationType.Scheduled,
    Username = Environment.UserName,
    FileList = _llmToolService.ReadEnvironment().ToArray()
};

await llmService.InvokeLlmAsync(invocationContext);

await app.RunAsync();
