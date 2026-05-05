# EmailServices.Api Dockerfile
# Multi-stage build for .NET 9 Web API

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["EmailServices.Api/EmailServices.Api.csproj", "EmailServices.Api/"]
COPY ["emailServices/emailServices.csproj", "emailServices/"]

# Restore dependencies
RUN dotnet restore "EmailServices.Api/EmailServices.Api.csproj"

# Copy source code
COPY . .
WORKDIR "/src/EmailServices.Api"

# Build the application
RUN dotnet build "EmailServices.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "EmailServices.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user and log directory
RUN addgroup --system --gid 1001 emailapi && \
    adduser --system --uid 1001 --ingroup emailapi emailapi && \
    apt-get update && apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/* && \
    mkdir /logs

# Copy published application
COPY --from=publish /app/publish .

# Set ownership and permissions
RUN chown -R emailapi:emailapi /app /logs
USER emailapi

# Configure environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "EmailServices.Api.dll"]
