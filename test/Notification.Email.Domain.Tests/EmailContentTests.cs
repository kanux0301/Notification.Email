using FluentAssertions;
using Notification.Email.Domain.ValueObjects;
using Xunit;

namespace Notification.Email.Domain.Tests;

public class EmailContentTests
{
    [Fact]
    public void Create_WithValidContent_ShouldSucceed()
    {
        // Arrange
        var subject = "Test Subject";
        var body = "Test Body";
        var isHtml = false;

        // Act
        var content = EmailContent.Create(subject, body, isHtml);

        // Assert
        content.Subject.Should().Be("Test Subject");
        content.Body.Should().Be("Test Body");
        content.IsHtml.Should().BeFalse();
    }

    [Fact]
    public void Create_WithHtmlContent_ShouldSetIsHtml()
    {
        // Arrange
        var subject = "Subject";
        var body = "<html><body>Hello</body></html>";
        var isHtml = true;

        // Act
        var content = EmailContent.Create(subject, body, isHtml);

        // Assert
        content.IsHtml.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNullSubject_ShouldSucceed()
    {
        // Arrange
        string? subject = null;
        var body = "Body content";

        // Act
        var content = EmailContent.Create(subject, body);

        // Assert
        content.Subject.Should().BeNull();
        content.Body.Should().Be("Body content");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyBody_ShouldThrow(string? body)
    {
        // Arrange & Act
        var act = () => EmailContent.Create("Subject", body!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*body cannot be empty*");
    }

    [Fact]
    public void Create_ShouldTrimSubject()
    {
        // Arrange
        var subject = "  Subject  ";
        var body = "Body";

        // Act
        var content = EmailContent.Create(subject, body);

        // Assert
        content.Subject.Should().Be("Subject");
    }

    [Fact]
    public void Equals_WithSameContent_ShouldBeEqual()
    {
        // Arrange
        var content1 = EmailContent.Create("Subject", "Body", true);
        var content2 = EmailContent.Create("Subject", "Body", true);

        // Act & Assert
        content1.Should().Be(content2);
    }

    [Fact]
    public void Equals_WithDifferentIsHtml_ShouldNotBeEqual()
    {
        // Arrange
        var content1 = EmailContent.Create("Subject", "Body", true);
        var content2 = EmailContent.Create("Subject", "Body", false);

        // Act & Assert
        content1.Should().NotBe(content2);
    }
}
