using Notification.Email.Application.Common;

namespace Notification.Email.Application.Emails.Commands.ProcessEmail;

public record ProcessEmailCommand(
    Guid NotificationId,
    string RecipientAddress,
    string? RecipientName,
    string? Subject,
    string Body,
    bool IsHtml,
    int Priority,
    Dictionary<string, string>? Metadata) : ICommand;
