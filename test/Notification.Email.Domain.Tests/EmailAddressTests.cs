using FluentAssertions;
using Notification.Email.Domain.ValueObjects;
using Xunit;

namespace Notification.Email.Domain.Tests;

public class EmailAddressTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@example.co.uk")]
    public void Create_WithValidEmail_ShouldSucceed(string email)
    {
        // Arrange & Act
        var result = EmailAddress.Create(email);

        // Assert
        result.Value.Should().Be(email.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyEmail_ShouldThrow(string? email)
    {
        // Arrange & Act
        var act = () => EmailAddress.Create(email!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    public void Create_WithInvalidFormat_ShouldThrow(string email)
    {
        // Arrange & Act
        var act = () => EmailAddress.Create(email);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email address format*");
    }

    [Fact]
    public void Create_ShouldNormalizeToLowerCase()
    {
        // Arrange
        var email = "Test@EXAMPLE.COM";

        // Act
        var result = EmailAddress.Create(email);

        // Assert
        result.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        var email = "  test@example.com  ";

        // Act
        var result = EmailAddress.Create(email);

        // Assert
        result.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void CreateOrDefault_WithInvalidEmail_ShouldReturnNull()
    {
        // Arrange
        var email = "invalid";

        // Act
        var result = EmailAddress.CreateOrDefault(email);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CreateOrDefault_WithValidEmail_ShouldReturnEmailAddress()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var result = EmailAddress.CreateOrDefault(email);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Equals_WithSameEmail_ShouldBeEqual()
    {
        // Arrange
        var email1 = EmailAddress.Create("test@example.com");
        var email2 = EmailAddress.Create("test@example.com");

        // Act & Assert
        email1.Should().Be(email2);
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com");

        // Act
        string value = email;

        // Assert
        value.Should().Be("test@example.com");
    }
}
