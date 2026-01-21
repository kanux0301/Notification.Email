using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Email.Domain.Enums;
using Notification.Email.Domain.Services;
using Notification.Email.Infrastructure.Configuration;

namespace Notification.Email.Infrastructure.Messaging;

public class AzureServiceBusStatusPublisher : IStatusPublisher, IAsyncDisposable
{
    private const string StatusTopicName = "notifications.status";

    private readonly ILogger<AzureServiceBusStatusPublisher> _logger;
    private readonly AzureServiceBusOptions _options;
    private ServiceBusClient? _client;
    private ServiceBusSender? _sender;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _disposed;

    public AzureServiceBusStatusPublisher(
        IOptions<AzureServiceBusOptions> options,
        ILogger<AzureServiceBusStatusPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishStatusAsync(
        Guid notificationId,
        EmailStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(cancellationToken);

        var statusMessage = new
        {
            NotificationId = notificationId,
            Status = (int)status,
            ErrorMessage = errorMessage,
            ProcessedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(statusMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var message = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Subject = status.ToString()
        };

        await _sender!.SendMessageAsync(message, cancellationToken);

        _logger.LogDebug(
            "Published status update for notification {NotificationId}: {Status}",
            notificationId,
            status);
    }

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_client is not null && _sender is not null)
            return;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_client is not null && _sender is not null)
                return;

            _client = new ServiceBusClient(_options.ConnectionString);
            _sender = _client.CreateSender(StatusTopicName);

            _logger.LogInformation("Azure Service Bus status publisher connected");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_sender is not null)
            await _sender.DisposeAsync();

        if (_client is not null)
            await _client.DisposeAsync();

        _connectionLock.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
