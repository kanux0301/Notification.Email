using Microsoft.Extensions.Logging;
using Notification.Email.Domain.Entities;
using Notification.Email.Domain.Services;

namespace Notification.Email.Infrastructure.Providers;

public class ConsoleEmailProvider : IEmailProvider
{
    private readonly ILogger<ConsoleEmailProvider> _logger;

    public ConsoleEmailProvider(ILogger<ConsoleEmailProvider> logger)
    {
        _logger = logger;
    }

    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "========== EMAIL SENT ==========\n" +
            "To: {To}\n" +
            "Subject: {Subject}\n" +
            "IsHtml: {IsHtml}\n" +
            "Priority: {Priority}\n" +
            "Body:\n{Body}\n" +
            "================================",
            message.Recipient.GetFormattedAddress(),
            message.Content.Subject ?? "(no subject)",
            message.Content.IsHtml,
            message.Priority,
            message.Content.Body);

        return Task.FromResult(new EmailSendResult(
            Success: true,
            MessageId: Guid.NewGuid().ToString()));
    }
}
