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

var host = Host.CreateApplicationBuilder(args);
host.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var services = host.Services;

services.Configure<ConfigurationModel>(host.Configuration.GetSection("ConfigurationModel"));
services.AddSingleton<IClock, Clock>();
services.AddSingleton<IFileSystem, FileSystem>();
services.AddSingleton<ILlmToolService, LlmToolService>();
services.AddSingleton<ISchedulingService, SchedulingService>();
services.AddHostedService<ChatService>();

services.AddLogging(configure => configure.AddConsole());

var app = host.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var configMonitor = app.Services.GetRequiredService<IOptionsMonitor<ConfigurationModel>>();
var clock = app.Services.GetRequiredService<IClock>();
var schedulingService = app.Services.GetRequiredService<ISchedulingService>();

logger.LogInformation($"Current Time: {clock.GetNow()}");

logger.LogInformation("Configuration Values:");
logger.LogInformation($"AllowedFilePathsForSearch: {string.Join(", ", configMonitor.CurrentValue.AllowedFilePathsForSearch)}");
logger.LogInformation($"CrontabForScheduledInvocation: {configMonitor.CurrentValue.CrontabForScheduledInvocation}");
logger.LogInformation($"ProbabilityOfStartingConversationsAutonomously: {configMonitor.CurrentValue.ProbabilityOfStartingConversationsAutonomously}");
logger.LogInformation($"TimeForExpectedReplyInConversation: {configMonitor.CurrentValue.TimeForExpectedReplyInConversation}");
logger.LogInformation($"AutonomousFeaturesEnabled: {configMonitor.CurrentValue.AutonomousFeaturesEnabled}");

logger.LogInformation($"Next Invocation Time: {schedulingService.GetNextInvocationTime()}");

await app.RunAsync();
