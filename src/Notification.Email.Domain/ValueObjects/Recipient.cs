using Notification.Email.Domain.Common;

namespace Notification.Email.Domain.ValueObjects;

public class Recipient : ValueObject
{
    public EmailAddress Address { get; }
    public string? Name { get; }

    private Recipient(EmailAddress address, string? name)
    {
        Address = address;
        Name = name;
    }

    public static Recipient Create(string email, string? name = null)
    {
        var address = EmailAddress.Create(email);
        return new Recipient(address, name?.Trim());
    }

    public string GetFormattedAddress()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? Address.Value
            : $"{Name} <{Address.Value}>";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Address;
        yield return Name;
    }

    public override string ToString() => GetFormattedAddress();
}
