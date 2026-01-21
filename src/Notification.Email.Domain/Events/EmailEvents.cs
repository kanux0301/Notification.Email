using Notification.Email.Domain.Common;

namespace Notification.Email.Domain.Events;

public record EmailReceivedEvent(Guid EmailId, Guid NotificationId) : DomainEvent;

public record EmailProcessingEvent(Guid EmailId, Guid NotificationId) : DomainEvent;

public record EmailSentEvent(Guid EmailId, Guid NotificationId, string? ExternalMessageId) : DomainEvent;

public record EmailDeliveredEvent(Guid EmailId, Guid NotificationId) : DomainEvent;

public record EmailFailedEvent(Guid EmailId, Guid NotificationId, string ErrorMessage, int RetryCount) : DomainEvent;
