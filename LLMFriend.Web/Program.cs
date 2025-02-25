using System.IO.Abstractions;
using LLMFriend.Configuration;
using LLMFriend.Web.Components;
using LLMFriend.Web.Services;
using LLMFriend.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHealthChecks();
builder.Services.AddHostedService<ScheduledBackgroundService>();
builder.Services.AddSingleton<ChatNotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<PersonalityService>();
builder.Services.AddSingleton<ILlmToolService, LlmToolService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IFileSystem, FileSystem>();

var configurationSection = builder.Configuration.GetRequiredSection("ConfigurationModel");

builder.Services.Configure<ConfigurationModel>(configurationSection);

var apiKey = configurationSection.GetRequiredSection("DeepseekApiKey").Value;
builder.Services.AddSingleton<Kernel>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<ILlmService, FakeLlmService>();
}
else
{
    builder.Services.AddSingleton<ILlmService, SemanticLlmService>();
}

#pragma warning disable SKEXP0010
builder.Services.AddOpenAIChatCompletion("deepseek-reasoner",
    new Uri("https://api.deepseek.com"),
    apiKey);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
