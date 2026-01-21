using Microsoft.Extensions.Logging;
using Notification.Email.Domain.Enums;
using Notification.Email.Domain.Services;

namespace Notification.Email.Infrastructure.Messaging;

public class ConsoleStatusPublisher : IStatusPublisher
{
    private readonly ILogger<ConsoleStatusPublisher> _logger;

    public ConsoleStatusPublisher(ILogger<ConsoleStatusPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishStatusAsync(
        Guid notificationId,
        EmailStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "========== STATUS UPDATE ==========\n" +
            "NotificationId: {NotificationId}\n" +
            "Status: {Status}\n" +
            "ErrorMessage: {ErrorMessage}\n" +
            "ProcessedAt: {ProcessedAt}\n" +
            "===================================",
            notificationId,
            status,
            errorMessage ?? "N/A",
            DateTime.UtcNow.ToString("O"));

        return Task.CompletedTask;
    }
}
