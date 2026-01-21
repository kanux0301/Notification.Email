# Notification.Email

Email channel microservice for the NotificationAPI orchestrator.

## Overview

This worker service consumes messages from the `notifications.email` queue, sends emails via the configured provider, and publishes status updates back to the orchestrator.

## Architecture

```
┌─────────────────────────┐
│ NotificationAPI         │
│ (Orchestrator)          │
└───────────┬─────────────┘
            │ publishes to
            ▼
┌─────────────────────────┐
│ notifications.email     │ ◄── RabbitMQ / Azure Service Bus
│ queue                   │
└───────────┬─────────────┘
            │ consumes
            ▼
┌─────────────────────────┐
│ Notification.Email      │
│ Worker                  │
│ ┌─────────────────────┐ │
│ │ IEmailProvider      │ │ ──► SMTP / Brevo / SendGrid / etc.
│ └─────────────────────┘ │
└───────────┬─────────────┘
            │ publishes status
            ▼
┌─────────────────────────┐
│ notifications.status    │ ──► Orchestrator updates DB
│ queue                   │
└─────────────────────────┘
```

## Project Structure (Clean Architecture + DDD)

```
Notification.Email/
├── src/
│   ├── Notification.Email.Domain/           # Domain layer
│   │   ├── Common/                          # Base classes (Entity, AggregateRoot, ValueObject)
│   │   ├── Entities/                        # EmailMessage aggregate root
│   │   ├── ValueObjects/                    # EmailAddress, EmailContent, Recipient
│   │   ├── Enums/                           # EmailStatus, EmailPriority
│   │   ├── Events/                          # Domain events
│   │   └── Services/                        # IEmailProvider, IStatusPublisher interfaces
│   │
│   ├── Notification.Email.Application/      # Application layer (CQRS)
│   │   ├── Common/                          # Result, Error, ICommand interfaces
│   │   ├── Behaviors/                       # ValidationBehavior (MediatR pipeline)
│   │   └── Emails/Commands/ProcessEmail/    # Command, Handler, Validator
│   │
│   ├── Notification.Email.Infrastructure/   # Infrastructure layer
│   │   ├── Configuration/                   # MessagingOptions, SmtpOptions
│   │   ├── Messaging/                       # RabbitMQ/Azure Service Bus consumers & publishers
│   │   └── Providers/                       # ConsoleEmailProvider, SmtpEmailProvider
│   │
│   └── Notification.Email.Worker/           # Host layer
│       ├── Program.cs                       # DI composition root
│       ├── Worker.cs                        # Background service
│       └── appsettings.json
│
├── test/
│   └── Notification.Email.Worker.Tests/
│       ├── Domain/                          # ValueObject & Entity tests
│       ├── Application/                     # Command & Validator tests
│       ├── Infrastructure/                  # Provider tests
│       └── Integration/                     # Mailpit integration tests
│
└── deployment/
    ├── build.yaml                           # Azure DevOps CI/CD pipeline
    └── Dockerfile                           # Container image definition
```

## Prerequisites

- .NET 10.0 SDK
- Docker (for RabbitMQ and Mailpit)

## Quick Start

### 1. Start RabbitMQ

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Access management UI at http://localhost:15672 (guest/guest)

### 2. Configure Secrets (for real email sending)

```bash
cd src/Notification.Email.Worker

# Initialize user secrets (if not already done)
dotnet user-secrets init

# Set your SMTP credentials (example with Brevo)
dotnet user-secrets set "Smtp:UserName" "your-smtp-username"
dotnet user-secrets set "Smtp:Password" "your-smtp-password"
dotnet user-secrets set "Smtp:FromAddress" "your-verified-email@example.com"
```

### 3. Run the Worker

```bash
cd src/Notification.Email.Worker
dotnet run
```

### 4. Send a Test Message

Via RabbitMQ Management UI (http://localhost:15672):
1. Go to Queues → `notifications.email`
2. Publish message:

```json
{
  "notificationId": "550e8400-e29b-41d4-a716-446655440000",
  "recipientAddress": "recipient@example.com",
  "recipientName": "Test User",
  "subject": "Test Email",
  "body": "<h1>Hello!</h1><p>This is a test email.</p>",
  "isHtml": true,
  "priority": 1,
  "metadata": {}
}
```

## Configuration

### appsettings.json

```json
{
  "Messaging": {
    "Provider": "RabbitMQ"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "ExchangeName": "notifications",
    "QueueName": "notifications.email"
  },
  "AzureServiceBus": {
    "ConnectionString": "",
    "TopicName": "notifications.email",
    "SubscriptionName": "email-worker"
  },
  "EmailProvider": {
    "Provider": "Smtp"
  },
  "Smtp": {
    "Host": "smtp-relay.brevo.com",
    "Port": 587,
    "UseSsl": true,
    "UserName": "",
    "Password": "",
    "FromAddress": "",
    "FromName": "Notification System"
  }
}
```

### Email Provider Options

| Provider | Config Value | Description |
|----------|--------------|-------------|
| Console | `"Provider": "Console"` | Logs emails to console (development) |
| SMTP | `"Provider": "Smtp"` | Sends via SMTP (Brevo, Gmail, etc.) |

### Supported SMTP Services

| Service | Host | Port |
|---------|------|------|
| Brevo | smtp-relay.brevo.com | 587 |
| Gmail | smtp.gmail.com | 587 |
| Mailpit (local) | localhost | 1025 |

## Testing

### Unit Tests

Run all unit tests:

```bash
dotnet test
```

Run with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories

| Category | Description | Command |
|----------|-------------|---------|
| Unit | Domain, Application, Infrastructure unit tests | `dotnet test --filter "Category!=Integration"` |
| Integration | End-to-end tests with Mailpit | `dotnet test --filter "Category=Integration"` |

### Integration Tests with Mailpit

Integration tests use [Mailpit](https://github.com/axllent/mailpit) as a fake SMTP server to verify emails are sent correctly.

#### 1. Start Mailpit

```bash
docker run -d --name mailpit -p 1025:1025 -p 8025:8025 axllent/mailpit
```

- SMTP server: `localhost:1025`
- Web UI: http://localhost:8025
- API: http://localhost:8025/api/v1

#### 2. Run Integration Tests

```bash
dotnet test --filter "Category=Integration"
```

#### 3. What Integration Tests Verify

- Emails are sent successfully via SMTP
- HTML content is preserved
- Multiple emails are delivered
- Email headers (To, From, Subject) are correct

#### Mailpit API Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /api/v1/messages` | List all captured emails |
| `GET /api/v1/message/{id}` | Get specific email details |
| `DELETE /api/v1/messages` | Clear all emails |
| `GET /api/v1/search?query=to:email@test.com` | Search emails |

## Build & Deploy

### Local Build

```bash
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

### Docker Build

```bash
docker build -f deployment/Dockerfile -t notification-email-worker .
docker run notification-email-worker
```

### CI/CD Pipeline

The `deployment/build.yaml` is configured for Azure DevOps with:
- Build and test on every PR
- Code coverage reporting
- Docker image build on main branch
- Artifact publishing

#### Running Integration Tests in CI/CD

Add Mailpit as a service in your pipeline:

```yaml
# Azure DevOps
services:
  mailpit:
    image: axllent/mailpit
    ports:
      - 1025:1025
      - 8025:8025

# GitHub Actions
services:
  mailpit:
    image: axllent/mailpit
    ports:
      - 1025:1025
      - 8025:8025
```

## Message Format

The worker expects messages in this format:

```json
{
  "notificationId": "guid",
  "recipientAddress": "user@example.com",
  "recipientName": "John Doe",
  "subject": "Welcome!",
  "body": "Hello, welcome to our platform!",
  "isHtml": false,
  "priority": 1,
  "metadata": {}
}
```

### Priority Values

| Value | Priority |
|-------|----------|
| 0 | Low |
| 1 | Normal |
| 2 | High |
| 3 | Critical |

## Status Updates

The worker publishes status updates to `notifications.status`:

```json
{
  "notificationId": "guid",
  "status": 2,
  "errorMessage": null,
  "processedAt": "2024-01-01T00:00:00Z"
}
```

### Status Values

| Value | Status | Description |
|-------|--------|-------------|
| 1 | Processing | Email is being processed |
| 2 | Sent | Email sent to SMTP server |
| 3 | Delivered | Delivery confirmed (if supported) |
| 4 | Failed | Email failed to send |

## Security

### Managing Secrets

Never commit secrets to the repository. Use one of these approaches:

#### Development (User Secrets)

```bash
cd src/Notification.Email.Worker
dotnet user-secrets set "Smtp:UserName" "your-username"
dotnet user-secrets set "Smtp:Password" "your-password"
dotnet user-secrets set "Smtp:FromAddress" "your-email@example.com"
```

#### Production (Environment Variables)

```bash
# Linux/macOS
export Smtp__UserName=your-username
export Smtp__Password=your-password
export Smtp__FromAddress=your-email@example.com

# Windows
set Smtp__UserName=your-username
set Smtp__Password=your-password
set Smtp__FromAddress=your-email@example.com
```

#### CI/CD (Pipeline Variables)

Use Azure DevOps Variable Groups or GitHub Secrets.

## Adding Custom Email Providers

Implement `IEmailProvider` interface:

```csharp
public class CustomEmailProvider : IEmailProvider
{
    public async Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken ct = default)
    {
        // Your implementation
        return new EmailSendResult(Success: true, MessageId: "msg-id");
    }
}
```

Register in `DependencyInjection.cs`:

```csharp
case "custom":
    services.AddSingleton<IEmailProvider, CustomEmailProvider>();
    break;
```

## License

MIT
