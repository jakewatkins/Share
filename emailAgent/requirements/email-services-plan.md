# Email Services Web API Implementation Plan (.NET 9 + Docker)

**Overview**: Containerized .NET 9 Web API for email operations (delete/move) supporting N8N workflows with comprehensive xUnit testing and lab deployment.

## **Phase 1: Project Structure & Core Setup**

### 1.1 Create Solution Structure
```
emailServices.Api/
├── EmailServices.Api.csproj          # .NET 9 Web API project
├── Program.cs                        # Minimal API configuration
├── appsettings.json                  # Base configuration
├── appsettings.Development.json      # Development overrides
├── Dockerfile                       # Multi-stage container build
├── .dockerignore                    # Container build optimization
└── Controllers/                     # (if needed for complex operations)

emailServices.Api.Tests/
├── EmailServices.Api.Tests.csproj   # xUnit test project
├── Unit/                           # Unit tests
│   ├── EmailOperationsServiceTests.cs
│   └── EndpointTests.cs
├── Integration/                    # Integration tests
│   ├── ApiIntegrationTests.cs
│   └── TestWebApplicationFactory.cs
└── Fixtures/                      # Test data and mocks
    ├── MockEmailServices.cs
    └── TestConfiguration.cs
```

### 1.2 Project Dependencies
**EmailServices.Api.csproj**:
- Microsoft.AspNetCore.App (framework reference)
- Project reference to `../emailServices/emailServices.csproj`
- Serilog.AspNetCore (structured logging)
- Swashbuckle.AspNetCore (OpenAPI/Swagger)
- Microsoft.AspNetCore.HealthChecks

**EmailServices.Api.Tests.csproj**:
- Microsoft.NET.Test.Sdk
- xunit, xunit.runner.visualstudio
- Microsoft.AspNetCore.Mvc.Testing (integration testing)
- Moq (mocking framework)
- FluentAssertions (test assertions)

## **Phase 2: API Endpoints Design**

### 2.1 REST API Design
```
DELETE /api/v1/emails/{emailId}?service={gmail|outlook}&userEmail={email}
PUT    /api/v1/emails/{emailId}/move?service={gmail|outlook}&userEmail={email}&folder={folderName}
GET    /health                    # Health check endpoint
GET    /health/ready              # Readiness probe for K8s
```

### 2.2 Request/Response Models
```csharp
// DTOs/EmailOperationRequest.cs
public record EmailDeleteRequest(string EmailId, string Service, string UserEmail);
public record EmailMoveRequest(string EmailId, string Service, string UserEmail, string DestinationFolder);

// DTOs/ApiResponse.cs
public record ApiResponse<T>(bool Success, T? Data = default, string? Error = null);
public record ErrorResponse(string Message, string? Details = null, string? CorrelationId = null);
```

### 2.3 Minimal API Endpoints
```csharp
// Program.cs excerpts
app.MapDelete("/api/v1/emails/{emailId}", DeleteEmailAsync);
app.MapPut("/api/v1/emails/{emailId}/move", MoveEmailAsync);
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = _ => false });
```

## **Phase 3: Email Operations Implementation**

### 3.1 Service Layer Architecture
```csharp
// Services/IEmailOperationsService.cs
public interface IEmailOperationsService
{
    Task<bool> DeleteEmailAsync(string emailId, string service, string userEmail);
    Task<bool> MoveEmailAsync(string emailId, string service, string userEmail, string destinationFolder);
}

// Services/EmailOperationsService.cs
public class EmailOperationsService : IEmailOperationsService
{
    // Orchestrates Gmail/Outlook service calls
    // Handles service-specific authentication
    // Provides unified error handling
}
```

### 3.2 Extended Email Service Methods (New Development)
**Add to emailServices library**:
```csharp
// In GmailService.cs - add new method
public async Task<bool> MoveEmailAsync(string emailId, string destinationLabel)
{
    // Implementation using Gmail API labels
    // Gmail doesn't have folders - uses labels for organization
}

// In OutlookService.cs - add new method
public async Task<bool> MoveEmailAsync(string emailId, string destinationFolderId)
{
    // Implementation using Microsoft Graph API
    // Move to specified folder by ID or name
}
```

### 3.3 Error Handling Strategy
```csharp
// Middleware/GlobalExceptionHandler.cs
public class GlobalExceptionHandler : IExceptionHandler
{
    // Log all exceptions with correlation IDs
    // Return HTTP 500 with sanitized error messages
    // Handle authentication failures specifically
}
```

## **Phase 4: Authentication & Configuration**

### 4.1 Container-Optimized Configuration
```csharp
// Configuration/ContainerConfiguration.cs
public class ContainerConfiguration
{
    // Environment variable-based configuration
    // Key Vault integration with DefaultAzureCredential
    // Secret mounting support for Docker secrets
}
```

### 4.2 Dependency Injection Setup
```csharp
// Program.cs DI registration
builder.Services.AddSingleton<IConfiguration>(containerConfig);
builder.Services.AddScoped<IEmailOperationsService, EmailOperationsService>();
builder.Services.AddScoped<IGmailService>(provider => /* factory pattern */);
builder.Services.AddScoped<IOutlookService>(provider => /* factory pattern */);
builder.Services.AddSingleton<IKeyVaultService, KeyVaultService>();
```

### 4.3 Logging Configuration
```csharp
// Serilog configuration for containers
builder.Host.UseSerilog((context, config) =>
    config.WriteTo.Console(new JsonFormatter())
          .WriteTo.File("/logs/emailapi.log", rollingInterval: RollingInterval.Day)
          .MinimumLevel.Information());
```

## **Phase 5: Testing Strategy**

### 5.1 Unit Tests
```csharp
// Tests/Unit/EmailOperationsServiceTests.cs
public class EmailOperationsServiceTests
{
    [Fact]
    public async Task DeleteEmailAsync_WithValidGmailId_ReturnsTrue() { }

    [Fact]
    public async Task MoveEmailAsync_WithValidOutlookFolder_ReturnsTrue() { }

    [Fact]
    public async Task DeleteEmailAsync_WithAuthFailure_ThrowsException() { }
}
```

### 5.2 Integration Tests
```csharp
// Tests/Integration/ApiIntegrationTests.cs
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task DELETE_EmailsEndpoint_ReturnsOk() { }

    [Fact]
    public async Task PUT_EmailsMoveEndpoint_ReturnsOk() { }
}
```

### 5.3 Test Infrastructure
- **TestWebApplicationFactory**: Configure test-specific services
- **MockEmailServices**: Mock Gmail/Outlook services for isolated testing
- **Test configuration**: In-memory configuration and secrets
- **Integration test data**: Sample email IDs and test accounts

## **Phase 6: Containerization**

### 6.1 Multi-Stage Dockerfile
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EmailServices.Api/EmailServices.Api.csproj", "EmailServices.Api/"]
COPY ["emailServices/emailServices.csproj", "emailServices/"]
RUN dotnet restore "EmailServices.Api/EmailServices.Api.csproj"
COPY . .
RUN dotnet build "EmailServices.Api/EmailServices.Api.csproj" -c Release -o /app/build
RUN dotnet publish "EmailServices.Api/EmailServices.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
RUN addgroup -g 1001 appuser && adduser -u 1001 -G appuser -s /bin/sh -D appuser
USER appuser
EXPOSE 8080
EXPOSE 8081
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EmailServices.Api.dll"]
```

### 6.2 Docker Compose for Development
```yaml
# docker-compose.yml
version: '3.8'
services:
  emailservices-api:
    build: .
    ports:
      - "8080:8080"
      - "8081:8081"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - AZURE_CLIENT_ID=${AZURE_CLIENT_ID}
      - AZURE_CLIENT_SECRET=${AZURE_CLIENT_SECRET}
      - AZURE_TENANT_ID=${AZURE_TENANT_ID}
      - KEYVAULT_NAME=${KEYVAULT_NAME}
    volumes:
      - ./logs:/logs
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

### 6.3 Container Security & Optimization
- **Non-root user**: Run as dedicated appuser (UID 1001)
- **Alpine base**: Smaller attack surface and image size
- **Multi-stage build**: Separate build and runtime environments
- **Health checks**: Container orchestration readiness
- **Log mounting**: Persistent logs for debugging

## **Phase 7: Deployment & Operations**

### 7.1 Container Registry & Deployment Scripts
```bash
#!/bin/bash
# scripts/deploy-to-lab.sh
docker build -t emailservices-api:latest .
docker tag emailservices-api:latest your-registry/emailservices-api:v1.0
docker push your-registry/emailservices-api:v1.0

# Deploy to lab environment
docker-compose -f docker-compose.lab.yml up -d
```

### 7.2 Lab Environment Configuration
```yaml
# docker-compose.lab.yml
version: '3.8'
services:
  emailservices-api:
    image: your-registry/emailservices-api:v1.0
    restart: unless-stopped
    ports:
      - "80:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    secrets:
      - azure_client_secret
      - keyvault_name
    volumes:
      - /var/log/emailservices:/logs

secrets:
  azure_client_secret:
    file: ./secrets/azure_client_secret.txt
  keyvault_name:
    file: ./secrets/keyvault_name.txt
```

### 7.3 N8N Integration Examples
```json
// N8N HTTP Request Node Configuration
{
  "method": "DELETE",
  "url": "http://your-lab-server/api/v1/emails/{{$json.emailId}}",
  "queryParameters": {
    "service": "gmail",
    "userEmail": "user@domain.com"
  },
  "headers": {
    "Content-Type": "application/json"
  }
}
```

## **Verification Checklist**

### Development Phase
- [ ] .NET 9 project builds successfully
- [ ] All xUnit tests pass (unit + integration)
- [ ] Swagger UI accessible at `/swagger`
- [ ] Health checks respond correctly

### Container Phase
- [ ] Docker image builds under 200MB
- [ ] Container runs with mounted secrets
- [ ] Health checks work in containerized environment
- [ ] Logs properly structured and accessible

### Deployment Phase
- [ ] Container deploys successfully to lab
- [ ] API endpoints accessible from N8N workflows
- [ ] Email operations work with real accounts
- [ ] Error handling returns HTTP 500 as specified
- [ ] Performance acceptable for automation workloads

## **Technology Stack Summary**

- **.NET 9**: Latest framework for optimal performance
- **Minimal APIs**: Simplified endpoint configuration
- **Serilog**: Structured logging for container environments
- **xUnit + Moq**: Comprehensive testing with mocking
- **Docker**: Multi-stage builds for production deployment
- **Swagger/OpenAPI**: Auto-generated documentation
- **Health Checks**: Container orchestration support

## **Key Implementation Notes**

1. **Gmail vs Outlook folders**: Gmail uses labels, Outlook uses actual folders - API should handle this difference transparently
2. **Authentication scope**: Reuse existing KeyVaultTokenCache patterns for token management
3. **Container secrets**: Support both environment variables and Docker secrets mounting
4. **Error correlation**: Include correlation IDs in all responses for debugging
5. **Rate limiting**: Consider implementing if needed for lab environment protection

This plan provides a production-ready, containerized email API that integrates seamlessly with your existing emailServices library while supporting modern deployment patterns for lab environments.
