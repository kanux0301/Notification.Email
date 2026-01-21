using Notification.Email.Domain.Entities;

namespace Notification.Email.Domain.Services;

public interface IEmailProvider
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public record EmailSendResult(
    bool Success,
    string? MessageId = null,
    string? ErrorMessage = null);
