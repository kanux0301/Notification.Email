using Notification.Email.Domain.Common;
using Notification.Email.Domain.Enums;
using Notification.Email.Domain.Events;
using Notification.Email.Domain.ValueObjects;

namespace Notification.Email.Domain.Entities;

public class EmailMessage : AggregateRoot
{
    public Guid NotificationId { get; private set; }
    public Recipient Recipient { get; private set; } = null!;
    public EmailContent Content { get; private set; } = null!;
    public EmailPriority Priority { get; private set; }
    public EmailStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string? ExternalMessageId { get; private set; }
    public Dictionary<string, string>? Metadata { get; private set; }

    private EmailMessage() : base() { }

    public static EmailMessage Create(
        Guid notificationId,
        string recipientEmail,
        string? recipientName,
        string? subject,
        string body,
        bool isHtml,
        EmailPriority priority,
        Dictionary<string, string>? metadata = null)
    {
        var message = new EmailMessage
        {
            NotificationId = notificationId,
            Recipient = Recipient.Create(recipientEmail, recipientName),
            Content = EmailContent.Create(subject, body, isHtml),
            Priority = priority,
            Status = EmailStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            Metadata = metadata
        };

        message.RaiseDomainEvent(new EmailReceivedEvent(message.Id, message.NotificationId));

        return message;
    }

    public void MarkAsProcessing()
    {
        if (Status != EmailStatus.Pending && Status != EmailStatus.Failed)
            throw new InvalidOperationException($"Cannot process email in status {Status}");

        Status = EmailStatus.Processing;
        ProcessedAt = DateTime.UtcNow;

        RaiseDomainEvent(new EmailProcessingEvent(Id, NotificationId));
    }

    public void MarkAsSent(string? externalMessageId = null)
    {
        if (Status != EmailStatus.Processing)
            throw new InvalidOperationException($"Cannot mark as sent email in status {Status}");

        Status = EmailStatus.Sent;
        SentAt = DateTime.UtcNow;
        ExternalMessageId = externalMessageId;
        ErrorMessage = null;

        RaiseDomainEvent(new EmailSentEvent(Id, NotificationId, externalMessageId));
    }

    public void MarkAsDelivered()
    {
        if (Status != EmailStatus.Sent)
            throw new InvalidOperationException($"Cannot mark as delivered email in status {Status}");

        Status = EmailStatus.Delivered;

        RaiseDomainEvent(new EmailDeliveredEvent(Id, NotificationId));
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = EmailStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;

        RaiseDomainEvent(new EmailFailedEvent(Id, NotificationId, errorMessage, RetryCount));
    }

    public bool CanRetry(int maxRetries = 3)
    {
        return Status == EmailStatus.Failed && RetryCount < maxRetries;
    }

    public void PrepareForRetry()
    {
        if (!CanRetry())
            throw new InvalidOperationException("Email cannot be retried");

        Status = EmailStatus.Pending;
        ErrorMessage = null;
    }
}
