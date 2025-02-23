using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.IO.Abstractions;
using System.Threading.Tasks;
using LLMFriend;

var host = Host.CreateApplicationBuilder(args);
host.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var services = host.Services;

services.Configure<AppSettings>(host.Configuration.GetSection("AppSettings"));
services.AddSingleton<IClock, Clock>();
services.AddSingleton<IFileSystem, FileSystem>();

services.AddLogging(configure => configure.AddConsole());

var app = host.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<AppSettings>>();
var clock = app.Services.GetRequiredService<IClock>();

logger.LogInformation(optionsMonitor.CurrentValue.Greeting);
logger.LogInformation($"Current Time: {clock.GetNow()}");

await app.RunAsync();