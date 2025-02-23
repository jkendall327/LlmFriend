using System;
using Xunit;
using Moq;
using LLMFriend.Services;
using LLMFriend.Configuration;
using Microsoft.Extensions.Logging;

namespace LLMFriend.Tests.Services
{
    public class SchedulingServiceTests
    {
        private readonly Mock<IClock> _clockMock;
        private readonly Mock<ILogger<SchedulingService>> _loggerMock;
        private readonly ConfigurationModel _config;

        public SchedulingServiceTests()
        {
            _clockMock = new Mock<IClock>();
            _loggerMock = new Mock<ILogger<SchedulingService>>();
            _config = new ConfigurationModel
            {
                CrontabForScheduledInvocation = "0 0 * * *" // Every day at midnight
            };
        }

        [Fact]
        public void GetNextInvocationTime_ShouldReturnCorrectTime()
        {
            // Arrange
            var currentTime = new DateTime(2023, 10, 1, 12, 0, 0);
            _clockMock.Setup(c => c.GetNow()).Returns(currentTime);

            var service = new SchedulingService(_config, _clockMock.Object, _loggerMock.Object);

            // Act
            var nextInvocation = service.GetNextInvocationTime();

            // Assert
            Assert.Equal(new DateTime(2023, 10, 2, 0, 0, 0), nextInvocation);
        }

        [Fact]
        public void Constructor_ShouldLogError_WhenCrontabIsInvalid()
        {
            // Arrange
            _config.CrontabForScheduledInvocation = "invalid_crontab";

            // Act & Assert
            var exception = Assert.Throws<CronFormatException>(() => new SchedulingService(_config, _clockMock.Object, _loggerMock.Object));

            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid crontab expression")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void GetNextInvocationTime_ShouldLogWarning_WhenNoNextOccurrence()
        {
            // Arrange
            _config.CrontabForScheduledInvocation = "0 0 31 2 *"; // February 31st
            var currentTime = new DateTime(2023, 2, 28, 12, 0, 0);
            _clockMock.Setup(c => c.GetNow()).Returns(currentTime);

            var service = new SchedulingService(_config, _clockMock.Object, _loggerMock.Object);

            // Act
            var nextInvocation = service.GetNextInvocationTime();

            // Assert
            Assert.Equal(DateTime.MinValue, nextInvocation);
            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No next occurrence")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // Additional test stubs can be added here
    }
}
