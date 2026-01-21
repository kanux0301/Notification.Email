using Notification.Email.Application;
using Notification.Email.Infrastructure;
using Notification.Email.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add DDD layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Background worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
