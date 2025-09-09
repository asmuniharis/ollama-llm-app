# Backend Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/OllamaLlmApp.Backend.csproj", "src/"]
RUN dotnet restore "src/OllamaLlmApp.Backend.csproj"

# Copy source code
COPY . .
WORKDIR "/src/src"

# Build application
RUN dotnet build "OllamaLlmApp.Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OllamaLlmApp.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "OllamaLlmApp.Backend.dll"]