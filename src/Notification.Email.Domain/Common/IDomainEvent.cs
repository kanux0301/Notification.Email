using MediatR;

namespace Notification.Email.Domain.Common;

public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
