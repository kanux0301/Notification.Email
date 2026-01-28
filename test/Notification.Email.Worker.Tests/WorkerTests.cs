using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Notification.Email.Application.Common.Messaging;
using Xunit;

namespace Notification.Email.Worker.Tests;

public class WorkerTests
{
    private readonly Mock<IMessageConsumer> _mockMessageConsumer;
    private readonly Mock<ILogger<Worker>> _mockLogger;

    public WorkerTests()
    {
        _mockMessageConsumer = new Mock<IMessageConsumer>();
        _mockLogger = new Mock<ILogger<Worker>>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStartMessageConsumer()
    {
        // Arrange
        _mockMessageConsumer
            .Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new Worker(_mockMessageConsumer.Object, _mockLogger.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        // Act
        try
        {
            await worker.StartAsync(cts.Token);
            await Task.Delay(50);
            await worker.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        _mockMessageConsumer.Verify(
            c => c.StartAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldStopMessageConsumer()
    {
        // Arrange
        _mockMessageConsumer
            .Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockMessageConsumer
            .Setup(c => c.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new Worker(_mockMessageConsumer.Object, _mockLogger.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        try
        {
            await worker.StartAsync(cts.Token);
            await Task.Delay(50);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Act
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _mockMessageConsumer.Verify(c => c.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogStartMessage()
    {
        // Arrange
        _mockMessageConsumer
            .Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new Worker(_mockMessageConsumer.Object, _mockLogger.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        // Act
        try
        {
            await worker.StartAsync(cts.Token);
            await Task.Delay(50);
            await worker.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("starting")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StopAsync_ShouldLogStopMessage()
    {
        // Arrange
        _mockMessageConsumer
            .Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockMessageConsumer
            .Setup(c => c.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new Worker(_mockMessageConsumer.Object, _mockLogger.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        try
        {
            await worker.StartAsync(cts.Token);
            await Task.Delay(50);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Act
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("stopping")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
