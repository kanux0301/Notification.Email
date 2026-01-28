using FluentAssertions;
using Notification.Email.Domain.ValueObjects;
using Xunit;

namespace Notification.Email.Domain.Tests;

public class RecipientTests
{
    [Fact]
    public void Create_WithEmailAndName_ShouldSucceed()
    {
        // Arrange
        var email = "test@example.com";
        var name = "John Doe";

        // Act
        var recipient = Recipient.Create(email, name);

        // Assert
        recipient.Address.Value.Should().Be("test@example.com");
        recipient.Name.Should().Be("John Doe");
    }

    [Fact]
    public void Create_WithEmailOnly_ShouldSucceed()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var recipient = Recipient.Create(email);

        // Assert
        recipient.Address.Value.Should().Be("test@example.com");
        recipient.Name.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        // Arrange
        var email = "test@example.com";
        var name = "  John Doe  ";

        // Act
        var recipient = Recipient.Create(email, name);

        // Assert
        recipient.Name.Should().Be("John Doe");
    }

    [Fact]
    public void GetFormattedAddress_WithName_ShouldReturnFormattedString()
    {
        // Arrange
        var recipient = Recipient.Create("test@example.com", "John Doe");

        // Act
        var formatted = recipient.GetFormattedAddress();

        // Assert
        formatted.Should().Be("John Doe <test@example.com>");
    }

    [Fact]
    public void GetFormattedAddress_WithoutName_ShouldReturnEmailOnly()
    {
        // Arrange
        var recipient = Recipient.Create("test@example.com");

        // Act
        var formatted = recipient.GetFormattedAddress();

        // Assert
        formatted.Should().Be("test@example.com");
    }

    [Fact]
    public void ToString_ShouldReturnFormattedAddress()
    {
        // Arrange
        var recipient = Recipient.Create("test@example.com", "John Doe");

        // Act
        var result = recipient.ToString();

        // Assert
        result.Should().Be("John Doe <test@example.com>");
    }

    [Fact]
    public void Create_WithInvalidEmail_ShouldThrow()
    {
        // Arrange
        var email = "invalid-email";
        var name = "John Doe";

        // Act
        var act = () => Recipient.Create(email, name);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var recipient1 = Recipient.Create("test@example.com", "John Doe");
        var recipient2 = Recipient.Create("test@example.com", "John Doe");

        // Act & Assert
        recipient1.Should().Be(recipient2);
    }
}
