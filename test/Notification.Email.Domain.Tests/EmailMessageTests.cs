using FluentAssertions;
using Notification.Email.Domain.Entities;
using Notification.Email.Domain.Enums;
using Notification.Email.Domain.Events;
using Xunit;

namespace Notification.Email.Domain.Tests;

public class EmailMessageTests
{
    [Fact]
    public void Create_ShouldInitializeCorrectly()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        // Act
        var message = EmailMessage.Create(
            notificationId: notificationId,
            recipientEmail: "test@example.com",
            recipientName: "John Doe",
            subject: "Test Subject",
            body: "Test Body",
            isHtml: false,
            priority: EmailPriority.Normal);

        // Assert
        message.NotificationId.Should().Be(notificationId);
        message.Recipient.Address.Value.Should().Be("test@example.com");
        message.Recipient.Name.Should().Be("John Doe");
        message.Content.Subject.Should().Be("Test Subject");
        message.Content.Body.Should().Be("Test Body");
        message.Content.IsHtml.Should().BeFalse();
        message.Priority.Should().Be(EmailPriority.Normal);
        message.Status.Should().Be(EmailStatus.Pending);
        message.RetryCount.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldRaiseEmailReceivedEvent()
    {
        // Arrange
        var notificationId = Guid.NewGuid();

        // Act
        var message = EmailMessage.Create(
            notificationId: notificationId,
            recipientEmail: "test@example.com",
            recipientName: null,
            subject: "Subject",
            body: "Body",
            isHtml: false,
            priority: EmailPriority.Low);

        // Assert
        message.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EmailReceivedEvent>();
    }

    [Fact]
    public void MarkAsProcessing_FromPending_ShouldSucceed()
    {
        // Arrange
        var message = CreateTestMessage();

        // Act
        message.MarkAsProcessing();

        // Assert
        message.Status.Should().Be(EmailStatus.Processing);
        message.ProcessedAt.Should().NotBeNull();
        message.DomainEvents.Should().Contain(e => e is EmailProcessingEvent);
    }

    [Fact]
    public void MarkAsProcessing_FromSent_ShouldThrow()
    {
        // Arrange
        var message = CreateTestMessage();
        message.MarkAsProcessing();
        message.MarkAsSent("msg-123");

        // Act
        var act = () => message.MarkAsProcessing();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot process email in status*");
    }

    [Fact]
    public void MarkAsSent_ShouldSucceed()
    {
        // Arrange
        var message = CreateTestMessage();
        message.MarkAsProcessing();

        // Act
        message.MarkAsSent("external-msg-id");

        // Assert
        message.Status.Should().Be(EmailStatus.Sent);
        message.SentAt.Should().NotBeNull();
        message.ExternalMessageId.Should().Be("external-msg-id");
        message.DomainEvents.Should().Contain(e => e is EmailSentEvent);
    }

    [Fact]
    public void MarkAsSent_FromPending_ShouldThrow()
    {
        // Arrange
        var message = CreateTestMessage();

        // Act
        var act = () => message.MarkAsSent("msg-123");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkAsDelivered_ShouldSucceed()
    {
        // Arrange
        var message = CreateTestMessage();
        message.MarkAsProcessing();
        message.MarkAsSent("msg-123");

        // Act
        message.MarkAsDelivered();

        // Assert
        message.Status.Should().Be(EmailStatus.Delivered);
        message.DomainEvents.Should().Contain(e => e is EmailDeliveredEvent);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetErrorAndIncrementRetry()
    {
        // Arrange
        var message = CreateTestMessage();
        message.MarkAsProcessing();

        // Act
        message.MarkAsFailed("SMTP connection failed");

        // Assert
        message.Status.Should().Be(EmailStatus.Failed);
        message.ErrorMessage.Should().Be("SMTP connection failed");
        message.RetryCount.Should().Be(1);
        message.DomainEvents.Should().Contain(e => e is EmailFailedEvent);
    }

    [Fact]
    public void CanRetry_WhenFailedAndUnderMaxRetries_ShouldReturnTrue()
    {
        // Arrange
        var message = CreateTestMessage();
        message.MarkAsProcessing();
        message.MarkAsFailed("Error");

        // Act
        var canRetry = message.CanRetry(maxRetries: 3);

        // Assert
        canRetry.Should().BeTrue();
    }

    [Fact]
    public void CanRetry_WhenMaxRetriesReached_ShouldReturnFalse()
    {
        // Arrange
        var message = CreateTestMessage();

        for (int i = 0; i < 3; i++)
        {
            message.MarkAsProcessing();
            message.MarkAsFailed("Error");
            if (i < 2) message.PrepareForRetry();
        }

        // Act
        var canRetry = message.CanRetry(maxRetries: 3);

        // Assert
        canRetry.Should().BeFalse();
        message.RetryCount.Should().Be(3);
    }

    [Fact]
    public void PrepareForRetry_ShouldResetToPending()
    {
        // Arrange
        var message = CreateTestMessage();
        message.MarkAsProcessing();
        message.MarkAsFailed("Error");

        // Act
        message.PrepareForRetry();

        // Assert
        message.Status.Should().Be(EmailStatus.Pending);
        message.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Create_WithMetadata_ShouldStoreMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            { "campaign", "welcome" },
            { "userId", "123" }
        };

        // Act
        var message = EmailMessage.Create(
            notificationId: Guid.NewGuid(),
            recipientEmail: "test@example.com",
            recipientName: null,
            subject: "Subject",
            body: "Body",
            isHtml: false,
            priority: EmailPriority.Normal,
            metadata: metadata);

        // Assert
        message.Metadata.Should().ContainKey("campaign");
        message.Metadata!["campaign"].Should().Be("welcome");
    }

    private static EmailMessage CreateTestMessage()
    {
        return EmailMessage.Create(
            notificationId: Guid.NewGuid(),
            recipientEmail: "test@example.com",
            recipientName: "Test User",
            subject: "Test Subject",
            body: "Test Body",
            isHtml: false,
            priority: EmailPriority.Normal);
    }
}
