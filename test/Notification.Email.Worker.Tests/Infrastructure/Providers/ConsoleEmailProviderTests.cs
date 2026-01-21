using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Notification.Email.Domain.Entities;
using Notification.Email.Domain.Enums;
using Notification.Email.Infrastructure.Providers;

namespace Notification.Email.Worker.Tests.Infrastructure.Providers;

public class ConsoleEmailProviderTests
{
    private readonly Mock<ILogger<ConsoleEmailProvider>> _loggerMock;
    private readonly ConsoleEmailProvider _provider;

    public ConsoleEmailProviderTests()
    {
        _loggerMock = new Mock<ILogger<ConsoleEmailProvider>>();
        _provider = new ConsoleEmailProvider(_loggerMock.Object);
    }

    [Fact]
    public async Task SendAsync_ShouldReturnSuccess()
    {
        // Arrange
        var message = CreateTestMessage();

        // Act
        var result = await _provider.SendAsync(message);

        // Assert
        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_ShouldReturnUniqueMessageId()
    {
        // Arrange
        var message = CreateTestMessage();

        // Act
        var result1 = await _provider.SendAsync(message);
        var result2 = await _provider.SendAsync(message);

        // Assert
        result1.MessageId.Should().NotBe(result2.MessageId);
    }

    [Fact]
    public async Task SendAsync_WithHtmlContent_ShouldSucceed()
    {
        // Arrange
        var message = EmailMessage.Create(
            notificationId: Guid.NewGuid(),
            recipientEmail: "test@example.com",
            recipientName: "Test User",
            subject: "HTML Email",
            body: "<html><body><h1>Hello</h1></body></html>",
            isHtml: true,
            priority: EmailPriority.Normal);
        message.MarkAsProcessing();

        // Act
        var result = await _provider.SendAsync(message);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WithHighPriority_ShouldSucceed()
    {
        // Arrange
        var message = EmailMessage.Create(
            notificationId: Guid.NewGuid(),
            recipientEmail: "test@example.com",
            recipientName: "Test User",
            subject: "Urgent",
            body: "This is urgent",
            isHtml: false,
            priority: EmailPriority.Critical);
        message.MarkAsProcessing();

        // Act
        var result = await _provider.SendAsync(message);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var message = CreateTestMessage();
        var cts = new CancellationTokenSource();

        // Act
        var result = await _provider.SendAsync(message, cts.Token);

        // Assert
        result.Success.Should().BeTrue();
    }

    private static EmailMessage CreateTestMessage()
    {
        var message = EmailMessage.Create(
            notificationId: Guid.NewGuid(),
            recipientEmail: "test@example.com",
            recipientName: "Test User",
            subject: "Test Subject",
            body: "Test Body",
            isHtml: false,
            priority: EmailPriority.Normal);
        message.MarkAsProcessing();
        return message;
    }
}
