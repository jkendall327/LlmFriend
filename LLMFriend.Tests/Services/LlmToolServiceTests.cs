using System;
using Xunit;
using Moq;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using LLMFriend.Services;
using System.Collections.Generic;

namespace LLMFriend.Tests.Services
{
    public class LlmToolServiceTests
    {
        private readonly Mock<IFileSystem> _fileSystemMock;
        private readonly Mock<ILogger<LlmToolService>> _loggerMock;
        private readonly Mock<IConfigurationModel> _configMock;
        private readonly LlmToolService _service;

        public LlmToolServiceTests()
        {
            _fileSystemMock = new Mock<IFileSystem>();
            _loggerMock = new Mock<ILogger<LlmToolService>>();
            _configMock = new Mock<IConfigurationModel>();
            _service = new LlmToolService(_fileSystemMock.Object, _loggerMock.Object, _configMock.Object);
        }

        [Fact]
        public void ReadEnvironment_ShouldReturnFilePaths_WhenFilesExist()
        {
            // Arrange
            _configMock.Setup(c => c.AllowedFilePathsForSearch).Returns(new List<string> { "path1", "path2" });
            _fileSystemMock.Setup(fs => fs.File.Exists("path1")).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.Exists("path2")).Returns(false);

            // Act
            var result = _service.ReadEnvironment();

            // Assert
            Assert.Single(result);
            Assert.Contains("path1", result);
        }

        [Fact]
        public void ReadFile_ShouldReturnContent_WhenFileExists()
        {
            // Arrange
            string filepath = "path/to/file.txt";
            string expectedContent = "File content";
            _fileSystemMock.Setup(fs => fs.File.Exists(filepath)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.ReadAllText(filepath)).Returns(expectedContent);

            // Act
            var result = _service.ReadFile(filepath);

            // Assert
            Assert.Equal(expectedContent, result);
        }

        [Fact]
        public void ReadFile_ShouldReturnEmptyString_WhenFileDoesNotExist()
        {
            // Arrange
            string filepath = "path/to/missingfile.txt";
            _fileSystemMock.Setup(fs => fs.File.Exists(filepath)).Returns(false);

            // Act
            var result = _service.ReadFile(filepath);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void StoreMemory_ShouldWriteMemoryToFile()
        {
            // Arrange
            string memory = "Some memory content";
            string memoryFolder = "memory/folder";
            _configMock.Setup(c => c.MemoryBankFolder).Returns(memoryFolder);
            _fileSystemMock.Setup(fs => fs.Directory.Exists(memoryFolder)).Returns(true);

            // Act
            _service.StoreMemory(memory);

            // Assert
            _fileSystemMock.Verify(fs => fs.File.WriteAllText(It.IsAny<string>(), memory), Times.Once);
        }

        [Fact]
        public void UpdatePersonality_ShouldWriteNewPersonality()
        {
            // Arrange
            string newPersonality = "New personality traits";
            string personalityPath = "path/to/personality.txt";
            _configMock.Setup(c => c.PersonalityProfilePath).Returns(personalityPath);

            // Act
            _service.UpdatePersonality(newPersonality);

            // Assert
            _fileSystemMock.Verify(fs => fs.File.WriteAllText(personalityPath, newPersonality), Times.Once);
        }
    }
}
