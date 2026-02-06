using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Notification.Email.Application.Common.Messaging;
using RabbitMQ.Client;

Console.WriteLine("=== Email Test Sender ===");
Console.WriteLine("This tool publishes an email message to RabbitMQ.");
Console.WriteLine("Make sure the Worker service is running to process it.\n");

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// Read RabbitMQ settings
var hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
var port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
var userName = configuration["RabbitMQ:UserName"] ?? "guest";
var password = configuration["RabbitMQ:Password"] ?? "guest";
var queueName = configuration["RabbitMQ:QueueName"] ?? "notifications.email";

Console.WriteLine($"RabbitMQ: {hostName}:{port} | Queue: {queueName}\n");

// Get email details
Console.Write("Enter recipient email address: ");
var recipientAddress = Console.ReadLine()?.Trim();

if (string.IsNullOrEmpty(recipientAddress))
{
    Console.WriteLine("Recipient email address is required!");
    return;
}

Console.Write("Enter recipient name (or press Enter to skip): ");
var recipientName = Console.ReadLine()?.Trim();

Console.Write("Enter email subject [Test Email]: ");
var subjectInput = Console.ReadLine()?.Trim();
var subject = string.IsNullOrEmpty(subjectInput) ? "Test Email" : subjectInput;

Console.Write("Enter email body (or press Enter for default): ");
var bodyInput = Console.ReadLine()?.Trim();
var body = string.IsNullOrEmpty(bodyInput)
    ? "<h1>Test Email</h1><p>This is a test email sent from the Notification.Email TestSender tool.</p><p>Sent at: " + DateTime.UtcNow.ToString("o") + "</p>"
    : bodyInput;

Console.Write("Is this HTML? (y/n) [y]: ");
var isHtmlInput = Console.ReadLine()?.Trim().ToLowerInvariant();
var isHtml = isHtmlInput != "n";

Console.Write("Enter priority (0=Low, 1=Normal, 2=High, 3=Critical) [1]: ");
var priorityInput = Console.ReadLine()?.Trim();
var priority = int.TryParse(priorityInput, out var p) ? p : 1;

// Create the message
var notificationId = Guid.NewGuid();
var message = new SendEmailMessage(
    NotificationId: notificationId,
    RecipientAddress: recipientAddress,
    RecipientName: string.IsNullOrEmpty(recipientName) ? null : recipientName,
    Subject: subject,
    Body: body,
    IsHtml: isHtml,
    Priority: priority,
    Metadata: new Dictionary<string, string>
    {
        { "source", "TestSender" },
        { "timestamp", DateTime.UtcNow.ToString("o") }
    });

var messageJson = JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });

Console.WriteLine($"\nNotification ID: {notificationId}");
Console.WriteLine($"To: {recipientAddress}");
Console.WriteLine($"Subject: {subject}");
Console.WriteLine($"HTML: {isHtml}\n");

// Publish to RabbitMQ
try
{
    var factory = new ConnectionFactory
    {
        HostName = hostName,
        Port = port,
        UserName = userName,
        Password = password
    };

    using var connection = await factory.CreateConnectionAsync();
    using var channel = await connection.CreateChannelAsync();

    await channel.QueueDeclareAsync(
        queue: queueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    var bodyBytes = Encoding.UTF8.GetBytes(messageJson);

    var properties = new BasicProperties
    {
        Persistent = true,
        ContentType = "application/json"
    };

    await channel.BasicPublishAsync(
        exchange: string.Empty,
        routingKey: queueName,
        mandatory: true,
        basicProperties: properties,
        body: bodyBytes);

    Console.WriteLine($"Message published to RabbitMQ queue '{queueName}' successfully!");
    Console.WriteLine("The Worker service will pick it up and send the email.");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to publish message to RabbitMQ: {ex.Message}");
    Console.WriteLine("\nMake sure RabbitMQ is running. You can start it with:");
    Console.WriteLine("  cd deployment && docker-compose up -d rabbitmq");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
