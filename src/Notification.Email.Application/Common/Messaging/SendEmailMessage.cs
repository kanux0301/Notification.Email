namespace Notification.Email.Application.Common.Messaging;

public record SendEmailMessage(
    Guid NotificationId,
    string RecipientAddress,
    string? RecipientName,
    string? Subject,
    string Body,
    bool IsHtml,
    int Priority,
    Dictionary<string, string>? Metadata);
