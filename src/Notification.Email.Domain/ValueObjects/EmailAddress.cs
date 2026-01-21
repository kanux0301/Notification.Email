using System.Text.RegularExpressions;
using Notification.Email.Domain.Common;

namespace Notification.Email.Domain.ValueObjects;

public partial class EmailAddress : ValueObject
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email address cannot be empty", nameof(email));

        email = email.Trim().ToLowerInvariant();

        if (!EmailRegex().IsMatch(email))
            throw new ArgumentException("Invalid email address format", nameof(email));

        return new EmailAddress(email);
    }

    public static EmailAddress? CreateOrDefault(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        try
        {
            return Create(email);
        }
        catch
        {
            return null;
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(EmailAddress email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
