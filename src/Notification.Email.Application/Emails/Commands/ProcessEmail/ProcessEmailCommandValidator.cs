using FluentValidation;

namespace Notification.Email.Application.Emails.Commands.ProcessEmail;

public class ProcessEmailCommandValidator : AbstractValidator<ProcessEmailCommand>
{
    public ProcessEmailCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty()
            .WithMessage("NotificationId is required");

        RuleFor(x => x.RecipientAddress)
            .NotEmpty()
            .WithMessage("RecipientAddress is required")
            .EmailAddress()
            .WithMessage("RecipientAddress must be a valid email address");

        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage("Body is required");

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 3)
            .WithMessage("Priority must be between 0 (Low) and 3 (Critical)");
    }
}
