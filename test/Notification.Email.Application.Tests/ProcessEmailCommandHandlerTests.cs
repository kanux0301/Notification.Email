using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Notification.Email.Application.Emails.Commands.ProcessEmail;
using Notification.Email.Domain.Entities;
using Notification.Email.Domain.Enums;
using Notification.Email.Domain.Services;
using Xunit;

namespace Notification.Email.Application.Tests;

public class ProcessEmailCommandHandlerTests
{
    private readonly Mock<IEmailProvider> _emailProviderMock;
    private readonly Mock<IStatusPublisher> _statusPublisherMock;
    private readonly Mock<ILogger<ProcessEmailCommandHandler>> _loggerMock;
    private readonly ProcessEmailCommandHandler _handler;

    public ProcessEmailCommandHandlerTests()
    {
        _emailProviderMock = new Mock<IEmailProvider>();
        _statusPublisherMock = new Mock<IStatusPublisher>();
        _loggerMock = new Mock<ILogger<ProcessEmailCommandHandler>>();

        _handler = new ProcessEmailCommandHandler(
            _emailProviderMock.Object,
            _statusPublisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenEmailSentSuccessfully_ShouldReturnSuccess()
    {
        // Arrange
        var command = CreateTestCommand();

        _emailProviderMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult(Success: true, MessageId: "msg-123"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenEmailSentSuccessfully_ShouldPublishProcessingAndSentStatus()
    {
        // Arrange
        var command = CreateTestCommand();

        _emailProviderMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult(Success: true, MessageId: "msg-123"));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _statusPublisherMock.Verify(
            x => x.PublishStatusAsync(
                command.NotificationId,
                EmailStatus.Processing,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _statusPublisherMock.Verify(
            x => x.PublishStatusAsync(
                command.NotificationId,
                EmailStatus.Sent,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailSendFails_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateTestCommand();

        _emailProviderMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult(Success: false, ErrorMessage: "SMTP error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Email.SendFailed");
    }

    [Fact]
    public async Task Handle_WhenEmailSendFails_ShouldPublishFailedStatus()
    {
        // Arrange
        var command = CreateTestCommand();
        var errorMessage = "SMTP connection refused";

        _emailProviderMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult(Success: false, ErrorMessage: errorMessage));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _statusPublisherMock.Verify(
            x => x.PublishStatusAsync(
                command.NotificationId,
                EmailStatus.Failed,
                errorMessage,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInvalidEmailAddress_ShouldReturnValidationError()
    {
        // Arrange
        var command = new ProcessEmailCommand(
            NotificationId: Guid.NewGuid(),
            RecipientAddress: "invalid-email",
            RecipientName: null,
            Subject: "Subject",
            Body: "Body",
            IsHtml: false,
            Priority: 1,
            Metadata: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation.Invalid");
    }

    [Fact]
    public async Task Handle_WhenProviderThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateTestCommand();

        _emailProviderMock
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Email.SendFailed");
    }

    private static ProcessEmailCommand CreateTestCommand()
    {
        return new ProcessEmailCommand(
            NotificationId: Guid.NewGuid(),
            RecipientAddress: "test@example.com",
            RecipientName: "Test User",
            Subject: "Test Subject",
            Body: "Test Body",
            IsHtml: false,
            Priority: 1,
            Metadata: null);
    }
}
