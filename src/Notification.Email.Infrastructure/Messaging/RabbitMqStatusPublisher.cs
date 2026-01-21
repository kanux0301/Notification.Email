using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Email.Domain.Enums;
using Notification.Email.Domain.Services;
using Notification.Email.Infrastructure.Configuration;
using RabbitMQ.Client;

namespace Notification.Email.Infrastructure.Messaging;

public class RabbitMqStatusPublisher : IStatusPublisher, IAsyncDisposable
{
    private const string StatusQueueName = "notifications.status";

    private readonly ILogger<RabbitMqStatusPublisher> _logger;
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _disposed;

    public RabbitMqStatusPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqStatusPublisher> logger)
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
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel!.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: StatusQueueName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogDebug(
            "Published status update for notification {NotificationId}: {Status}",
            notificationId,
            status);
    }

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            return;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
                return;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            _logger.LogInformation(
                "RabbitMQ status publisher connected to {Host}:{Port}",
                _options.HostName,
                _options.Port);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _connectionLock.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
