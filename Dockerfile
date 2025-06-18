# ARM64-optimized Dockerfile for PlatformFlower API
# Use ARM64-specific base images for better performance
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-arm64v8 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy-arm64v8 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file and restore dependencies for ARM64
COPY ["PlatformFlower/PlatformFlower.csproj", "PlatformFlower/"]
RUN dotnet restore "PlatformFlower/PlatformFlower.csproj" --runtime linux-arm64

# Copy source code
COPY . .
WORKDIR "/src/PlatformFlower"

# Build for ARM64 with optimizations
RUN dotnet build "./PlatformFlower.csproj" \
    -c "${BUILD_CONFIGURATION}" \
    -o /app/build \
    --runtime linux-arm64 \
    --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
# Publish for ARM64 with optimizations
RUN dotnet publish "./PlatformFlower.csproj" \
    -c "${BUILD_CONFIGURATION}" \
    -o /app/publish \
    --runtime linux-arm64 \
    --self-contained false \
    --no-restore \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false \
    /p:PublishReadyToRun=false

FROM base AS final
WORKDIR /app

# Install curl for health checks
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER app

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables for ARM64 optimization
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PlatformFlower.dll"]
