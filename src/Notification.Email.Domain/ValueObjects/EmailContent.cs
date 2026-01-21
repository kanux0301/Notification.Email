using Notification.Email.Domain.Common;

namespace Notification.Email.Domain.ValueObjects;

public class EmailContent : ValueObject
{
    public string? Subject { get; }
    public string Body { get; }
    public bool IsHtml { get; }

    private EmailContent(string? subject, string body, bool isHtml)
    {
        Subject = subject;
        Body = body;
        IsHtml = isHtml;
    }

    public static EmailContent Create(string? subject, string body, bool isHtml = false)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Email body cannot be empty", nameof(body));

        return new EmailContent(subject?.Trim(), body, isHtml);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Subject;
        yield return Body;
        yield return IsHtml;
    }
}
