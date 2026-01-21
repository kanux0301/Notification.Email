using FluentAssertions;
using Notification.Email.Application.Emails.Commands.ProcessEmail;

namespace Notification.Email.Worker.Tests.Application.Commands;

public class ProcessEmailCommandValidatorTests
{
    private readonly ProcessEmailCommandValidator _validator;

    public ProcessEmailCommandValidatorTests()
    {
        _validator = new ProcessEmailCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new ProcessEmailCommand(
            NotificationId: Guid.NewGuid(),
            RecipientAddress: "test@example.com",
            RecipientName: "Test User",
            Subject: "Test Subject",
            Body: "Test Body",
            IsHtml: false,
            Priority: 1,
            Metadata: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyNotificationId_ShouldFail()
    {
        // Arrange
        var command = new ProcessEmailCommand(
            NotificationId: Guid.Empty,
            RecipientAddress: "test@example.com",
            RecipientName: null,
            Subject: "Subject",
            Body: "Body",
            IsHtml: false,
            Priority: 1,
            Metadata: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NotificationId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyRecipientAddress_ShouldFail(string? address)
    {
        // Arrange
        var command = new ProcessEmailCommand(
            NotificationId: Guid.NewGuid(),
            RecipientAddress: address!,
            RecipientName: null,
            Subject: "Subject",
            Body: "Body",
            IsHtml: false,
            Priority: 1,
            Metadata: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientAddress");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public async Task Validate_WithInvalidEmailFormat_ShouldFail(string email)
    {
        // Arrange
        var command = new ProcessEmailCommand(
            NotificationId: Guid.NewGuid(),
            RecipientAddress: email,
            RecipientName: null,
            Subject: "Subject",
            Body: "Body",
            IsHtml: false,
            Priority: 1,
            Metadata: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientAddress");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyBody_ShouldFail(string? body)
    {
        // Arrange
        var command = new ProcessEmailCommand(
            NotificationId: Guid.NewGuid(),
            RecipientAddress: "test@example.com",
            RecipientName: null,
            Subject: "Subject",
            Body: body!,
            IsHtml: false,
            Priority: 1,
            Metadata: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    public async Task Validate_WithInvalidPriority_ShouldFail(int priority)
    {
        // Arrange
        var command = new ProcessEmailCommand(
            NotificationId: Guid.NewGuid(),
            RecipientAddress: "test@example.com",
            RecipientName: null,
            Subject: "Subject",
            Body: "Body",
            IsHtml: false,
            Priority: priority,
            Metadata: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Priority");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Validate_WithValidPriority_ShouldPass(int priority)
    {
        // Arrange
        var command = new ProcessEmailCommand(
            NotificationId: Guid.NewGuid(),
            RecipientAddress: "test@example.com",
            RecipientName: null,
            Subject: "Subject",
            Body: "Body",
            IsHtml: false,
            Priority: priority,
            Metadata: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
