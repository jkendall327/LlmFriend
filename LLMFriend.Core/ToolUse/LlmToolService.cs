using System.ComponentModel;
using System.IO.Abstractions;
using LLMFriend.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace LLMFriend.Services
{
    public class LlmToolService : ILlmToolService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<LlmToolService> _logger;
        private readonly AppConfiguration _config;

        public LlmToolService(IFileSystem fileSystem, ILogger<LlmToolService> logger, IOptionsMonitor<AppConfiguration> config)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _config = config.CurrentValue;
        }

        [KernelFunction]
        [Description("Gets the list of files the assistant is allowed to view.")]
        public List<string> ReadEnvironment()
        {
            var filePaths = new List<string>();
            try
            {
                foreach (var path in _config.AllowedFilePathsForSearch)
                {
                    if (_fileSystem.File.Exists(path))
                    {
                        filePaths.Add(path);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reading environment: {ErrorMessage}", ex.Message);
                return new List<string>();
            }

            return filePaths;
        }

        [KernelFunction]
        [Description("Gets the content of a file for the assistant.")]
        public string ReadFile(string filepath)
        {
            try
            {
                if (_fileSystem.File.Exists(filepath))
                {
                    return _fileSystem.File.ReadAllText(filepath);
                }
                else
                {
                    _logger.LogWarning("File not found: {FilePath}", filepath);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reading file {FilePath}: {ErrorMessage}", filepath, ex.Message);
                return string.Empty;
            }
        }

        public void StoreMemory(string memory)
        {
            try
            {
                var memoryPath = _config.MemoryBankFolder;

                if (memoryPath is null)
                {
                    return;
                }
                
                if (!_fileSystem.Directory.Exists(memoryPath))
                {
                    _fileSystem.Directory.CreateDirectory(memoryPath);
                }

                var fileName = $"memory_{Guid.NewGuid()}.txt";
                var fullPath = _fileSystem.Path.Combine(memoryPath, fileName);
                _fileSystem.File.WriteAllText(fullPath, memory);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error storing memory: {ErrorMessage}", ex.Message);
                // Silently ignore as per specification
            }
        }

        [KernelFunction]
        [Description("Updates the assistant's personality.")]
        public void UpdatePersonality(string newPersonality)
        {
            try
            {
                var personalityPath = _config.PersonalityProfilePath;
                _fileSystem.File.WriteAllText(personalityPath, newPersonality);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating personality: {ErrorMessage}", ex.Message);
                // Silently ignore as per specification
            }
        }
    }
}
