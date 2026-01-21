# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY src/Notification.Email.Worker/Notification.Email.Worker.csproj src/Notification.Email.Worker/
RUN dotnet restore src/Notification.Email.Worker/Notification.Email.Worker.csproj

# Copy source code and build
COPY src/ src/
RUN dotnet publish src/Notification.Email.Worker/Notification.Email.Worker.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD pgrep -f "Notification.Email.Worker" || exit 1

ENTRYPOINT ["dotnet", "Notification.Email.Worker.dll"]
