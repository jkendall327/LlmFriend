using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

public class LLMBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IChatCompletionService _chatService;
    private readonly string _configPath;
    private readonly string _watchFolder;
    private readonly TimeSpan _timeout;
    
    public LLMBackgroundService(
        IConfiguration configuration,
        IChatCompletionService chatService)
    {
        _configuration = configuration;
        _chatService = chatService;
        
        // Read settings from configuration
        _configPath = _configuration["LLMService:ConfigPath"] 
            ?? throw new ArgumentNullException("ConfigPath must be specified in configuration");
        _watchFolder = _configuration["LLMService:WatchFolder"] 
            ?? throw new ArgumentNullException("WatchFolder must be specified in configuration");
        _timeout = TimeSpan.FromSeconds(
            int.Parse(_configuration["LLMService:TimeoutSeconds"] ?? "30"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Read config file
                var config = await ReadConfigAsync();
                
                // Process files in watch folder
                var files = Directory.GetFiles(_watchFolder);
                foreach (var file in files)
                {
                    var content = await File.ReadAllTextAsync(file, stoppingToken);
                    
                    // Create chat context with file content and config
                    // var chatContext = new ChatContext
                    // {
                    //     Content = content,
                    //     Config = config
                    // };
                    
                    // Set timeout for chat completion
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    cts.CancelAfter(_timeout);
                    
                    try
                    {
                        // var response = await _chatService.GetChatCompletionAsync(
                        //     chatContext, 
                        //     cts.Token);
                        //     
                        // // Handle response...
                        // await ProcessResponseAsync(response, file);
                    }
                    catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                    {
                        // Handle timeout
                        await HandleTimeoutAsync(file);
                    }
                }
                
                // Wait until next scheduled time
                await WaitUntilNextExecutionTimeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Log error and continue
                // Consider implementing retry logic
            }
        }
    }
    
    private async Task<Dictionary<string, object>> ReadConfigAsync()
    {
        var jsonString = await File.ReadAllTextAsync(_configPath);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString) 
            ?? new Dictionary<string, object>();
    }
    
    private async Task WaitUntilNextExecutionTimeAsync(CancellationToken token)
    {
        var executionTime = TimeOnly.Parse(_configuration["LLMService:ExecutionTime"] ?? "00:00");
        var now = TimeOnly.FromDateTime(DateTime.Now);
        var tomorrow = DateTime.Today.AddDays(1);
        
        var nextRun = now <= executionTime 
            ? DateTime.Today.Add(executionTime.ToTimeSpan())
            : tomorrow.Add(executionTime.ToTimeSpan());
            
        var delay = nextRun - DateTime.Now;
        await Task.Delay(delay, token);
    }
    
    private async Task ProcessResponseAsync(string response, string filePath)
    {
        // Implement response handling logic
        // For example, save to output file or trigger other actions
        await File.WriteAllTextAsync(
            Path.Combine(_watchFolder, $"response_{Path.GetFileName(filePath)}"),
            response);
    }
    
    private async Task HandleTimeoutAsync(string filePath)
    {
        // Implement timeout handling logic
        // For example, move file to error folder or retry queue
        var errorFolder = Path.Combine(_watchFolder, "errors");
        Directory.CreateDirectory(errorFolder);
        
        var errorPath = Path.Combine(errorFolder, Path.GetFileName(filePath));
        File.Move(filePath, errorPath, true);
    }
}