using System.IO.Abstractions;
using LLMFriend.Configuration;
using LLMFriend.Web.Components;
using LLMFriend.Web.Services;
using LLMFriend.Services;
using Microsoft.Extensions.Options;
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

builder.Services.Configure<AppConfiguration>(
    builder.Configuration.GetRequiredSection(nameof(AppConfiguration)));
builder.Services.Configure<AiModelOptions>(
    builder.Configuration.GetRequiredSection(AiModelOptions.ConfigurationSection));

builder.Services.AddSingleton<Kernel>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<ILlmService, FakeLlmService>();
}
else
{
    builder.Services.AddSingleton<ILlmService, SemanticLlmService>();
}

builder.Services.AddSingleton(sp => {
    var aiModelOptions = sp.GetRequiredService<IOptions<AiModelOptions>>().Value;
    
    #pragma warning disable SKEXP0010

    if (aiModelOptions.ApiRootUrl is not null)
    {
        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                aiModelOptions.ModelName,
                new Uri(aiModelOptions.ApiRootUrl),
                aiModelOptions.ApiKey)
            .Build();
    
        return kernel;
    }
    else
    {
        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                aiModelOptions.ModelName,
                aiModelOptions.ApiKey)
            .Build();
    
        return kernel;
    }
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
