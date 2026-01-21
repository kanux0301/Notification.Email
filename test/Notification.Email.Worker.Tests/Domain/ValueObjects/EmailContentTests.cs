using FluentAssertions;
using Notification.Email.Domain.ValueObjects;

namespace Notification.Email.Worker.Tests.Domain.ValueObjects;

public class EmailContentTests
{
    [Fact]
    public void Create_WithValidContent_ShouldSucceed()
    {
        // Act
        var content = EmailContent.Create("Test Subject", "Test Body", false);

        // Assert
        content.Subject.Should().Be("Test Subject");
        content.Body.Should().Be("Test Body");
        content.IsHtml.Should().BeFalse();
    }

    [Fact]
    public void Create_WithHtmlContent_ShouldSetIsHtml()
    {
        // Act
        var content = EmailContent.Create("Subject", "<html><body>Hello</body></html>", true);

        // Assert
        content.IsHtml.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNullSubject_ShouldSucceed()
    {
        // Act
        var content = EmailContent.Create(null, "Body content");

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
        // Act
        var act = () => EmailContent.Create("Subject", body!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*body cannot be empty*");
    }

    [Fact]
    public void Create_ShouldTrimSubject()
    {
        // Act
        var content = EmailContent.Create("  Subject  ", "Body");

        // Assert
        content.Subject.Should().Be("Subject");
    }

    [Fact]
    public void Equals_WithSameContent_ShouldBeEqual()
    {
        // Arrange
        var content1 = EmailContent.Create("Subject", "Body", true);
        var content2 = EmailContent.Create("Subject", "Body", true);

        // Assert
        content1.Should().Be(content2);
    }

    [Fact]
    public void Equals_WithDifferentIsHtml_ShouldNotBeEqual()
    {
        // Arrange
        var content1 = EmailContent.Create("Subject", "Body", true);
        var content2 = EmailContent.Create("Subject", "Body", false);

        // Assert
        content1.Should().NotBe(content2);
    }
}
