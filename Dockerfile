# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

# Multi-stage build for optimized Docker image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

# Copy project file
COPY ["dotnet-auth-server.csproj", "./"]

# Restore NuGet packages
RUN dotnet restore "dotnet-auth-server.csproj"

# Copy source code
COPY . .

# Build in Release mode
RUN dotnet build "dotnet-auth-server.csproj" \
    -c Release \
    -o /app/build \
    --no-restore

# Publish stage
FROM builder AS publish

RUN dotnet publish "dotnet-auth-server.csproj" \
    -c Release \
    -o /app/publish \
    --no-build

# Runtime stage - use smaller base image
FROM mcr.microsoft.com/dotnet/aspnet:10.0

# Create non-root user for security
RUN useradd -m -u 1001 appuser

WORKDIR /app

# Copy published files from publish stage
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=https://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f https://localhost:8080/health || exit 1

# Run application
ENTRYPOINT ["dotnet", "DotnetAuthServer.dll"]
