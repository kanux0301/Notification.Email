using Notification.Email.Application.Common.Messaging;

namespace Notification.Email.Worker;

public class Worker : BackgroundService
{
    private readonly IMessageConsumer _messageConsumer;
    private readonly ILogger<Worker> _logger;

    public Worker(IMessageConsumer messageConsumer, ILogger<Worker> logger)
    {
        _messageConsumer = messageConsumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Worker starting...");

        try
        {
            await _messageConsumer.StartAsync(stoppingToken);

            _logger.LogInformation("Email Worker is now processing messages");

            // Keep the worker running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Email Worker shutdown requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email Worker encountered an error");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email Worker stopping...");

        await _messageConsumer.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);

        _logger.LogInformation("Email Worker stopped");
    }
}
