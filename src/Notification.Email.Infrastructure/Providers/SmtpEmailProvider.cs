using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Notification.Email.Domain.Entities;
using Notification.Email.Domain.Services;
using Notification.Email.Infrastructure.Configuration;

namespace Notification.Email.Infrastructure.Providers;

public class SmtpEmailProvider : IEmailProvider
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(
        IOptions<SmtpOptions> options,
        ILogger<SmtpEmailProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mimeMessage = CreateMimeMessage(message);

            using var client = new SmtpClient();

            _logger.LogDebug(
                "Connecting to SMTP server {Host}:{Port}",
                _options.Host,
                _options.Port);

            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrEmpty(_options.UserName))
            {
                await client.AuthenticateAsync(
                    _options.UserName,
                    _options.Password,
                    cancellationToken);
            }

            var response = await client.SendAsync(mimeMessage, cancellationToken);

            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully to {Recipient}. MessageId: {MessageId}",
                message.Recipient.GetFormattedAddress(),
                mimeMessage.MessageId);

            return new EmailSendResult(
                Success: true,
                MessageId: mimeMessage.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {Recipient}",
                message.Recipient.GetFormattedAddress());

            return new EmailSendResult(
                Success: false,
                ErrorMessage: ex.Message);
        }
    }

    private MimeMessage CreateMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(new MailboxAddress(
            _options.FromName,
            _options.FromAddress));

        mimeMessage.To.Add(new MailboxAddress(
            message.Recipient.Name,
            message.Recipient.Address.Value));

        mimeMessage.Subject = message.Content.Subject ?? string.Empty;

        var bodyBuilder = new BodyBuilder();

        if (message.Content.IsHtml)
        {
            bodyBuilder.HtmlBody = message.Content.Body;
        }
        else
        {
            bodyBuilder.TextBody = message.Content.Body;
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        // Add priority header
        mimeMessage.Priority = message.Priority switch
        {
            Domain.Enums.EmailPriority.Low => MessagePriority.NonUrgent,
            Domain.Enums.EmailPriority.Normal => MessagePriority.Normal,
            Domain.Enums.EmailPriority.High => MessagePriority.Urgent,
            Domain.Enums.EmailPriority.Critical => MessagePriority.Urgent,
            _ => MessagePriority.Normal
        };

        // Add notification ID as custom header for tracking
        mimeMessage.Headers.Add("X-Notification-Id", message.NotificationId.ToString());

        return mimeMessage;
    }
}
