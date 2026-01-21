using System.Text.Json;
using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Email.Application.Common.Messaging;
using Notification.Email.Application.Emails.Commands.ProcessEmail;
using Notification.Email.Infrastructure.Configuration;

namespace Notification.Email.Infrastructure.Messaging;

public class AzureServiceBusMessageConsumer : IMessageConsumer, IAsyncDisposable
{
    private readonly ILogger<AzureServiceBusMessageConsumer> _logger;
    private readonly AzureServiceBusOptions _options;
    private readonly IMediator _mediator;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;
    private bool _disposed;

    public AzureServiceBusMessageConsumer(
        IOptions<AzureServiceBusOptions> options,
        IMediator mediator,
        ILogger<AzureServiceBusMessageConsumer> logger)
    {
        _options = options.Value;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client = new ServiceBusClient(_options.ConnectionString);

        _processor = _client.CreateProcessor(
            _options.TopicName,
            _options.SubscriptionName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 10,
                PrefetchCount = 10
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(cancellationToken);

        _logger.LogInformation(
            "Azure Service Bus consumer started, listening on topic {TopicName}/{SubscriptionName}",
            _options.TopicName,
            _options.SubscriptionName);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var message = JsonSerializer.Deserialize<SendEmailMessage>(
                args.Message.Body.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (message is not null)
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

                var result = await _mediator.Send(command, args.CancellationToken);

                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "Email processing failed for notification {NotificationId}: {Error}",
                        message.NotificationId,
                        result.Error?.Message);
                }
            }

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Error in Azure Service Bus processor: {ErrorSource}",
            args.ErrorSource);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
        }

        _logger.LogInformation("Azure Service Bus consumer stopped");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_processor is not null)
            await _processor.DisposeAsync();

        if (_client is not null)
            await _client.DisposeAsync();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
