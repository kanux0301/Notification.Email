using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Email.Application.Common.Messaging;
using Notification.Email.Application.Emails.Commands.ProcessEmail;
using Notification.Email.Infrastructure.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notification.Email.Infrastructure.Messaging;

public class RabbitMqMessageConsumer : IMessageConsumer, IAsyncDisposable
{
    private readonly ILogger<RabbitMqMessageConsumer> _logger;
    private readonly RabbitMqOptions _options;
    private readonly IMediator _mediator;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public RabbitMqMessageConsumer(
        IOptions<RabbitMqOptions> options,
        IMediator mediator,
        ILogger<RabbitMqMessageConsumer> logger)
    {
        _options = options.Value;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
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

        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: _options.QueueName,
            exchange: _options.ExchangeName,
            routingKey: _options.QueueName,
            cancellationToken: cancellationToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<SendEmailMessage>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (message is not null)
                {
                    await ProcessMessageAsync(message, cancellationToken);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "RabbitMQ consumer started, listening on queue {QueueName}",
            _options.QueueName);
    }

    private async Task ProcessMessageAsync(SendEmailMessage message, CancellationToken cancellationToken)
    {
        var command = new ProcessEmailCommand(
            NotificationId: message.NotificationId,
            RecipientAddress: message.RecipientAddress,
            RecipientName: message.RecipientName,
            Subject: message.Subject,
            Body: message.Body,
            IsHtml: message.IsHtml,
            Priority: message.Priority,
            Metadata: message.Metadata);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Email processing failed for notification {NotificationId}: {Error}",
                message.NotificationId,
                result.Error?.Message);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
        }

        _logger.LogInformation("RabbitMQ consumer stopped");
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

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
