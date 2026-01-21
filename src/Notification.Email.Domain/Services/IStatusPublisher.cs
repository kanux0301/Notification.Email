using Notification.Email.Domain.Enums;

namespace Notification.Email.Domain.Services;

public interface IStatusPublisher
{
    Task PublishStatusAsync(
        Guid notificationId,
        EmailStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}
