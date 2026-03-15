# EmailServices.Api - Docker Deployment Guide

## Overview
This guide covers Docker containerization for the EmailServices.Api, supporting both development and production/lab environments.

## Quick Start

### Development
```bash
# Start development environment
./scripts/docker-dev.sh up

# View logs
./scripts/docker-dev.sh logs

# Test API
./scripts/docker-dev.sh test

# Stop environment
./scripts/docker-dev.sh down
```

### Production/Lab
```bash
# Deploy to production
./scripts/docker-prod.sh deploy

# Check status
./scripts/docker-prod.sh status

# Monitor health
./scripts/docker-prod.sh health
```

## Architecture

### Multi-Stage Dockerfile
- **Build Stage**: Uses .NET 9 SDK to build the application
- **Publish Stage**: Creates optimized release build
- **Runtime Stage**: Minimal ASP.NET Core runtime with security hardening

### Security Features
- Runs as non-root user (`emailapi:emailapi`)
- Minimal attack surface with distroless-style base image
- Health checks for container orchestration
- Readonly filesystem mounts where possible

## Environments

### Development (`docker-compose.yml`)
- **Port**: 5000 → 8080
- **Logging**: Debug level for troubleshooting
- **Volumes**: Local settings and log mounting
- **Features**: Hot reloading, verbose logging

### Production (`docker-compose.prod.yml`)
- **Port**: 80 → 8080
- **Logging**: Warning level for performance
- **Security**: Resource limits, health checks
- **Features**: Traefik reverse proxy support

## API Endpoints

### Core Endpoints
- `DELETE /api/v1/emails/{emailId}` - Delete email from specified service
- `PUT /api/v1/emails/{emailId}/move` - Move email to folder (NotImplemented)

### Operational Endpoints
- `GET /health` - Basic health check
- `GET /health/ready` - Readiness probe
- `GET /swagger` - API documentation (dev only)

## Configuration

### Environment Variables
| Variable | Development | Production | Description |
|----------|-------------|------------|--------------|
| `ASPNETCORE_ENVIRONMENT` | Development | Production | Runtime environment |
| `ASPNETCORE_URLS` | http://+:8080 | http://+:8080 | Binding configuration |
| `keyvaultName` | kvGPSecrets | kvGPSecrets | Azure Key Vault name |

### Volume Mounts
- **Development**: `./settings.json:/app/settings.json:ro`
- **Production**: `./settings.production.json:/app/settings.json:ro`
- **Logs**: `./logs:/app/logs` (dev) or `./data/logs:/app/logs` (prod)

## Health Checks

### Container Health
```bash
# Docker health check
curl -f http://localhost:8080/health || exit 1

# Kubernetes readiness
curl -f http://localhost:8080/health/ready
```

### Monitoring
- **Interval**: 30s (dev) / 60s (prod)
- **Timeout**: 3s (dev) / 10s (prod)
- **Retries**: 3-5 attempts
- **Start Period**: 5s (dev) / 30s (prod)

## Troubleshooting

### Common Issues

1. **Port Conflicts**
   ```bash
   # Check port usage
   lsof -i :5000  # Development
   lsof -i :80    # Production
   ```

2. **Key Vault Access**
   ```bash
   # Check Azure credentials
   az account show

   # Test Key Vault access
   az keyvault secret list --vault-name kvGPSecrets
   ```

3. **Container Logs**
   ```bash
   # Development logs
   ./scripts/docker-dev.sh logs

   # Production logs
   ./scripts/docker-prod.sh logs
   ```

4. **Health Check Failures**
   ```bash
   # Manual health check
   curl -v http://localhost:5000/health

   # Check container status
   docker ps
   docker inspect emailservices-api-dev
   ```

### Debug Commands
```bash
# Enter running container
docker exec -it emailservices-api-dev /bin/bash

# Check container resources
docker stats emailservices-api-dev

# View detailed container info
docker inspect emailservices-api-dev
```

## Integration with N8N

### Webhook Configuration
```json
{
  "method": "DELETE",
  "url": "http://emailapi.lab.local/api/v1/emails/{{$json.emailId}}",
  "qs": {
    "service": "{{$json.service}}",
    "userEmail": "{{$json.userEmail}}"
  }
}
```

### Example N8N Workflow
1. **Trigger**: Webhook or schedule
2. **HTTP Request**: Call EmailServices.Api
3. **Conditional**: Check response status
4. **Notification**: Success/failure handling

## Deployment Checklist

- [ ] Azure Key Vault access configured
- [ ] Production settings file created
- [ ] Docker daemon running
- [ ] Ports available (80 for prod, 5000 for dev)
- [ ] Log directories created
- [ ] Scripts have execute permissions

```bash
# Set script permissions
chmod +x scripts/docker-dev.sh scripts/docker-prod.sh
```

## Next Steps

1. **Implement Move Operations**: Add Gmail/Outlook folder move functionality
2. **Add Monitoring**: Integrate with Prometheus/Grafana
3. **Security Hardening**: Add OAuth2/JWT authentication
4. **CI/CD Pipeline**: Automate Docker builds and deployments
