namespace Notification.Email.Application.Common;

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static class Email
    {
        public static Error NotFound(Guid id) =>
            new("Email.NotFound", $"Email with ID '{id}' was not found");

        public static Error InvalidRecipient(string email) =>
            new("Email.InvalidRecipient", $"Invalid email address: '{email}'");

        public static Error SendFailed(string reason) =>
            new("Email.SendFailed", $"Failed to send email: {reason}");

        public static Error AlreadyProcessed(Guid id) =>
            new("Email.AlreadyProcessed", $"Email with ID '{id}' has already been processed");

        public static Error InvalidStatus(string currentStatus, string expectedStatus) =>
            new("Email.InvalidStatus", $"Email is in '{currentStatus}' status, expected '{expectedStatus}'");
    }

    public static class Validation
    {
        public static Error Required(string fieldName) =>
            new("Validation.Required", $"'{fieldName}' is required");

        public static Error Invalid(string fieldName, string reason) =>
            new("Validation.Invalid", $"'{fieldName}' is invalid: {reason}");
    }
}
