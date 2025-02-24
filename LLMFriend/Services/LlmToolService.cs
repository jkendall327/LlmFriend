using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using LLMFriend.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMFriend.Services
{
    public class LlmToolService : ILlmToolService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<LlmToolService> _logger;
        private readonly ConfigurationModel _config;

        public LlmToolService(IFileSystem fileSystem, ILogger<LlmToolService> logger, IOptionsMonitor<ConfigurationModel> config)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _config = config.CurrentValue;
        }

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
