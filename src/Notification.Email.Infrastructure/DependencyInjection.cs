using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Email.Application.Common.Messaging;
using Notification.Email.Domain.Services;
using Notification.Email.Infrastructure.Configuration;
using Notification.Email.Infrastructure.Messaging;
using Notification.Email.Infrastructure.Providers;

namespace Notification.Email.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMessaging(configuration);
        services.AddEmailProvider(configuration);

        return services;
    }

    private static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var messagingProvider = configuration.GetValue<string>("Messaging:Provider") ?? "InMemory";

        switch (messagingProvider.ToLowerInvariant())
        {
            case "rabbitmq":
                services.Configure<RabbitMqOptions>(
                    configuration.GetSection(RabbitMqOptions.SectionName));
                services.AddSingleton<IMessageConsumer, RabbitMqMessageConsumer>();
                services.AddSingleton<IStatusPublisher, RabbitMqStatusPublisher>();
                break;

            case "azureservicebus":
                services.Configure<AzureServiceBusOptions>(
                    configuration.GetSection(AzureServiceBusOptions.SectionName));
                services.AddSingleton<IMessageConsumer, AzureServiceBusMessageConsumer>();
                services.AddSingleton<IStatusPublisher, AzureServiceBusStatusPublisher>();
                break;

            case "inmemory":
            default:
                services.Configure<RabbitMqOptions>(
                    configuration.GetSection(RabbitMqOptions.SectionName));
                services.AddSingleton<IMessageConsumer, RabbitMqMessageConsumer>();
                services.AddSingleton<IStatusPublisher, ConsoleStatusPublisher>();
                break;
        }

        return services;
    }

    private static IServiceCollection AddEmailProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var emailProvider = configuration.GetValue<string>("EmailProvider:Provider") ?? "Console";

        switch (emailProvider.ToLowerInvariant())
        {
            case "smtp":
                services.Configure<SmtpOptions>(
                    configuration.GetSection(SmtpOptions.SectionName));
                services.AddSingleton<IEmailProvider, SmtpEmailProvider>();
                break;

            case "console":
            default:
                services.AddSingleton<IEmailProvider, ConsoleEmailProvider>();
                break;
        }

        return services;
    }
}
