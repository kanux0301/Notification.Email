using Microsoft.Extensions.Logging;
using Notification.Email.Application.Common;
using Notification.Email.Domain.Entities;
using Notification.Email.Domain.Enums;
using Notification.Email.Domain.Services;

namespace Notification.Email.Application.Emails.Commands.ProcessEmail;

public class ProcessEmailCommandHandler : ICommandHandler<ProcessEmailCommand>
{
    private readonly IEmailProvider _emailProvider;
    private readonly IStatusPublisher _statusPublisher;
    private readonly ILogger<ProcessEmailCommandHandler> _logger;

    public ProcessEmailCommandHandler(
        IEmailProvider emailProvider,
        IStatusPublisher statusPublisher,
        ILogger<ProcessEmailCommandHandler> logger)
    {
        _emailProvider = emailProvider;
        _statusPublisher = statusPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(ProcessEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing email notification {NotificationId} to {Recipient}",
            request.NotificationId,
            request.RecipientAddress);

        try
        {
            // Create domain entity
            var emailMessage = EmailMessage.Create(
                notificationId: request.NotificationId,
                recipientEmail: request.RecipientAddress,
                recipientName: request.RecipientName,
                subject: request.Subject,
                body: request.Body,
                isHtml: request.IsHtml,
                priority: (EmailPriority)request.Priority,
                metadata: request.Metadata);

            // Mark as processing and notify orchestrator
            emailMessage.MarkAsProcessing();
            await _statusPublisher.PublishStatusAsync(
                request.NotificationId,
                EmailStatus.Processing,
                cancellationToken: cancellationToken);

            // Send via email provider
            var result = await _emailProvider.SendAsync(emailMessage, cancellationToken);

            if (result.Success)
            {
                emailMessage.MarkAsSent(result.MessageId);
                await _statusPublisher.PublishStatusAsync(
                    request.NotificationId,
                    EmailStatus.Sent,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Email sent successfully for notification {NotificationId}, MessageId: {MessageId}",
                    request.NotificationId,
                    result.MessageId);

                return Result.Success();
            }
            else
            {
                emailMessage.MarkAsFailed(result.ErrorMessage ?? "Unknown error");
                await _statusPublisher.PublishStatusAsync(
                    request.NotificationId,
                    EmailStatus.Failed,
                    result.ErrorMessage,
                    cancellationToken);

                _logger.LogWarning(
                    "Email send failed for notification {NotificationId}: {Error}",
                    request.NotificationId,
                    result.ErrorMessage);

                return Result.Failure(Error.Email.SendFailed(result.ErrorMessage ?? "Unknown error"));
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid email data for notification {NotificationId}", request.NotificationId);

            await _statusPublisher.PublishStatusAsync(
                request.NotificationId,
                EmailStatus.Failed,
                ex.Message,
                cancellationToken);

            return Result.Failure(Error.Validation.Invalid("Email", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email for notification {NotificationId}", request.NotificationId);

            await _statusPublisher.PublishStatusAsync(
                request.NotificationId,
                EmailStatus.Failed,
                ex.Message,
                cancellationToken);

            return Result.Failure(Error.Email.SendFailed(ex.Message));
        }
    }
}
