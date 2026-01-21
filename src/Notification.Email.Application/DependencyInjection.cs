using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Notification.Email.Application.Behaviors;

namespace Notification.Email.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Singleton);

        return services;
    }
}
