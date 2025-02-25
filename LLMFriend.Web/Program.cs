using LLMFriend.Web.Components;
using LLMFriend.Web.Services;
using LLMFriend;
using LLMFriend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHostedService<ScheduledBackgroundService>();
builder.Services.AddSingleton<ChatNotificationService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddSingleton<ILlmService, SemanticLlmService>();
builder.Services.AddSingleton<ILlmToolService, LlmToolService>();
builder.Services.AddSingleton(TimeProvider.System);

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

app.Run();
