using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Email.Domain.Entities;
using Notification.Email.Domain.Enums;
using Notification.Email.Infrastructure.Configuration;
using Notification.Email.Infrastructure.Providers;
using Xunit;

namespace Notification.Email.Integration.Tests;

/// <summary>
/// Integration tests using Mailpit as a fake SMTP server.
///
/// Prerequisites:
/// 1. Start Mailpit: docker run -d --name mailpit -p 1025:1025 -p 8025:8025 axllent/mailpit
/// 2. Run these tests
///
/// These tests are marked with [Trait("Category", "Integration")] so they can be
/// excluded from regular unit test runs: dotnet test --filter "Category!=Integration"
/// </summary>
public class EmailIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _mailpitClient;
    private readonly SmtpEmailProvider _emailProvider;
    private const string MailpitApiUrl = "http://localhost:8025/api/v1";

    public EmailIntegrationTests()
    {
        _mailpitClient = new HttpClient { BaseAddress = new Uri(MailpitApiUrl) };

        var options = Options.Create(new SmtpOptions
        {
            Host = "localhost",
            Port = 1025,
            UseSsl = false,
            FromAddress = "test@notification.local",
            FromName = "Test Sender"
        });

        var logger = LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger<SmtpEmailProvider>();

        _emailProvider = new SmtpEmailProvider(options, logger);
    }

    public async Task InitializeAsync()
    {
        // Clear all emails before each test
        try
        {
            await _mailpitClient.DeleteAsync("/messages");
        }
        catch (HttpRequestException)
        {
            // Mailpit not running - tests will be skipped
        }
    }

    public Task DisposeAsync()
    {
        _mailpitClient.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendEmail_ShouldBeReceivedByMailpit()
    {
        // Skip if Mailpit is not running
        if (!await IsMailpitRunning())
        {
            return;
        }

        // Arrange
        var message = EmailMessage.Create(
            notificationId: Guid.NewGuid(),
            recipientEmail: "recipient@example.com",
            recipientName: "Test Recipient",
            subject: "Integration Test Email",
            body: "This is a test email from integration tests.",
            isHtml: false,
            priority: EmailPriority.Normal);
        message.MarkAsProcessing();

        // Act
        var result = await _emailProvider.SendAsync(message);

        // Assert
        result.Success.Should().BeTrue();

        // Wait a bit for email to be received
        await Task.Delay(500);

        // Verify email was received by Mailpit
        var emails = await GetMailpitMessages();
        emails.Should().ContainSingle();

        var receivedEmail = emails.First();
        receivedEmail.Subject.Should().Be("Integration Test Email");
        receivedEmail.To.Should().Contain(t => t.Address == "recipient@example.com");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendHtmlEmail_ShouldPreserveHtmlContent()
    {
        // Skip if Mailpit is not running
        if (!await IsMailpitRunning())
        {
            return;
        }

        // Arrange
        var htmlBody = "<h1>Welcome!</h1><p>This is an <strong>HTML</strong> email.</p>";
        var message = EmailMessage.Create(
            notificationId: Guid.NewGuid(),
            recipientEmail: "html-test@example.com",
            recipientName: "HTML Recipient",
            subject: "HTML Test Email",
            body: htmlBody,
            isHtml: true,
            priority: EmailPriority.High);
        message.MarkAsProcessing();

        // Act
        var result = await _emailProvider.SendAsync(message);

        // Assert
        result.Success.Should().BeTrue();

        await Task.Delay(500);

        var emails = await GetMailpitMessages();
        emails.Should().ContainSingle();

        // Get full message to check HTML content
        var fullMessage = await GetMailpitMessage(emails.First().Id);
        fullMessage.Html.Should().Contain("<h1>Welcome!</h1>");
        fullMessage.Html.Should().Contain("<strong>HTML</strong>");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendMultipleEmails_AllShouldBeReceived()
    {
        // Skip if Mailpit is not running
        if (!await IsMailpitRunning())
        {
            return;
        }

        // Arrange
        var recipients = new[] { "user1@test.com", "user2@test.com", "user3@test.com" };

        // Act
        foreach (var recipient in recipients)
        {
            var message = EmailMessage.Create(
                notificationId: Guid.NewGuid(),
                recipientEmail: recipient,
                recipientName: recipient.Split('@')[0],
                subject: $"Test for {recipient}",
                body: "Bulk test",
                isHtml: false,
                priority: EmailPriority.Normal);
            message.MarkAsProcessing();

            await _emailProvider.SendAsync(message);
        }

        // Assert
        await Task.Delay(1000);

        var emails = await GetMailpitMessages();
        emails.Should().HaveCount(3);
        emails.Select(e => e.To.First().Address)
            .Should().BeEquivalentTo(recipients);
    }

    private async Task<bool> IsMailpitRunning()
    {
        try
        {
            var response = await _mailpitClient.GetAsync("/messages");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<MailpitMessageSummary>> GetMailpitMessages()
    {
        var response = await _mailpitClient.GetFromJsonAsync<MailpitMessagesResponse>("/messages");
        return response?.Messages ?? new List<MailpitMessageSummary>();
    }

    private async Task<MailpitMessage> GetMailpitMessage(string id)
    {
        var response = await _mailpitClient.GetFromJsonAsync<MailpitMessage>($"/message/{id}");
        return response!;
    }

    // Mailpit API response models
    private record MailpitMessagesResponse(List<MailpitMessageSummary> Messages, int Total);

    private record MailpitMessageSummary(
        string Id,
        string Subject,
        List<MailpitAddress> From,
        List<MailpitAddress> To);

    private record MailpitMessage(
        string Id,
        string Subject,
        List<MailpitAddress> From,
        List<MailpitAddress> To,
        string Text,
        string Html);

    private record MailpitAddress(string Name, string Address);
}
