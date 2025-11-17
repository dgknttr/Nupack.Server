# Multi-stage Dockerfile for Nupack Server
# Builds both API server and Web UI for production deployment

# Build stage - Use SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY ["src/Nupack.Server.Api/Nupack.Server.Api.csproj", "src/Nupack.Server.Api/"]
COPY ["src/Nupack.Server.Web/Nupack.Server.Web.csproj", "src/Nupack.Server.Web/"]

# Restore dependencies for both projects
RUN dotnet restore "src/Nupack.Server.Api/Nupack.Server.Api.csproj"
RUN dotnet restore "src/Nupack.Server.Web/Nupack.Server.Web.csproj"

# Copy source code
COPY . .

# Build and publish the API server
WORKDIR "/src/src/Nupack.Server.Api"
RUN dotnet publish "Nupack.Server.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/server

# Build and publish the Web UI
WORKDIR "/src/src/Nupack.Server.Web"
RUN dotnet publish "Nupack.Server.Web.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/web

# Runtime stage - Use minimal runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Create application directory
WORKDIR /app

# Create non-root user and data directories with proper permissions
RUN groupadd -r appgroup && useradd -r -g appgroup appuser && \
    mkdir -p /app/data/packages /app/server /app/web && \
    chown -R appuser:appgroup /app

# Copy published applications
COPY --from=build --chown=appuser:appgroup /app/server ./server/
COPY --from=build --chown=appuser:appgroup /app/web ./web/

# Copy startup script
COPY --chown=appuser:appgroup <<EOF /app/start.sh
#!/bin/bash
set -e

# Start API server in background
cd /app/server
dotnet Nupack.Server.Api.dll --urls "https://0.0.0.0:5001;http://0.0.0.0:5003" &
SERVER_PID=\$!

# Start Web UI in background
cd /app/web
dotnet Nupack.Server.Web.dll --urls "https://0.0.0.0:5002;http://0.0.0.0:5004" &
WEB_PID=\$!

# Wait for any process to exit
wait -n

# Exit with status of process that exited first
exit \$?
EOF

RUN chmod +x /app/start.sh

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    Branding__ProductName="Nupack Server" \
    Branding__CompanyName="Your Organization"

# Health check for both services
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:5003/health && curl -f http://localhost:5004/health || exit 1

# Expose ports for both services
EXPOSE 5001 5002 5003 5004

# Switch to non-root user
USER appuser

# Set entrypoint to startup script
ENTRYPOINT ["/app/start.sh"]

# Metadata labels
LABEL maintainer="Nupack Server Contributors" \
      version="1.0.0" \
      description="Nupack Server - A modern, self-hosted NuGet v3 server with web interface" \
      org.opencontainers.image.title="Nupack Server" \
      org.opencontainers.image.description="A modern, self-hosted NuGet v3 server implementation built with .NET 9" \
      org.opencontainers.image.version="1.0.0" \
      org.opencontainers.image.vendor="Nupack Server Contributors" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.source="https://github.com/dgknttr/Nupack.Server"
