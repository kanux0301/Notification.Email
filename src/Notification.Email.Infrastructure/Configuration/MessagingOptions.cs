namespace Notification.Email.Infrastructure.Configuration;

public class MessagingOptions
{
    public const string SectionName = "Messaging";

    public string Provider { get; set; } = "InMemory";
}

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "notifications";
    public string QueueName { get; set; } = "notifications.email";
}

public class AzureServiceBusOptions
{
    public const string SectionName = "AzureServiceBus";

    public string ConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = "notifications.email";
    public string SubscriptionName { get; set; } = "email-worker";
}

public class EmailProviderOptions
{
    public const string SectionName = "EmailProvider";

    public string Provider { get; set; } = "Console";
}

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; } = false;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@notification.local";
    public string FromName { get; set; } = "Notification System";
}
