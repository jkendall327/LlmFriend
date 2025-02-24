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
services.AddSingleton<ILlmService, SemanticLlmService>();
services.AddSingleton<ChatService>();

var apiKey = host.Configuration.GetRequiredSection("ConfigurationModel").GetRequiredSection("OpenAIApiKey").Value;
services.AddOpenAIChatCompletion("davinci", apiKey);
services.AddSingleton<Kernel>();

var app = host.Build();

var chat = app.Services.GetRequiredService<ChatService>();

await chat.RunChatAsync();
