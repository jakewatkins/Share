# N8N with HTTPS using Traefik

This setup provides N8N with HTTPS support using Traefik as a reverse proxy, perfect for OAuth integrations like Slack triggers that require secure redirect URLs.

## Quick Start

1. **Update hostname** (optional):
   ```bash
   # Edit .env file to set your hostname
   HOSTNAME=fairladyz.local
   ```

2. **Add hostname to your hosts file**:
   ```bash
   sudo echo "127.0.0.1 fairladyz.local n8n.fairladyz.local traefik.fairladyz.local" >> /etc/hosts
   ```

3. **Generate self-signed certificates**:
   ```bash
   ./generate-certs.sh
   ```

4. **Start the services**:
   ```bash
   docker-compose up -d
   ```

5. **Access the applications**:
   - **N8N**: https://n8n.fairladyz.local (admin/changeme123)
   - **Traefik Dashboard**: https://traefik.fairladyz.local

## Services Included

### Traefik (Reverse Proxy)
- Automatic HTTPS with self-signed certificates
- HTTP to HTTPS redirection
- Dashboard for monitoring routes
- Ready for adding more services

### N8N (Workflow Automation)
- Latest version with HTTPS support
- Basic authentication enabled
- Webhook URL configured for HTTPS
- Two persistent volumes (data and logs)
- Optimized for OAuth integrations

## Directory Structure

```
.
├── docker-compose.yml       # Main configuration
├── .env                     # Environment variables
├── generate-certs.sh        # Certificate generation script
├── traefik-data/           # Traefik configuration data
├── certs/                  # SSL certificates
├── n8n-data/               # N8N data persistence
└── n8n-logs/               # N8N logs
```

## Customization

### Change Default Credentials
Edit `.env` file:
```env
N8N_BASIC_AUTH_USER=your-username
N8N_BASIC_AUTH_PASSWORD=your-secure-password
```

### Add More Services
The setup uses the `lab-network` Docker network. To add new services:

```yaml
services:
  your-new-service:
    image: your-image
    networks:
      - lab-network
    labels:
      - traefik.enable=true
      - traefik.http.routers.yourservice.rule=Host(`yourservice.${HOSTNAME}`)
      - traefik.http.routers.yourservice.entrypoints=websecure
      - traefik.http.routers.yourservice.tls=true
```

### Certificate Management
- Certificates are valid for 365 days
- Run `./generate-certs.sh` to regenerate if needed
- Certificates include SAN entries for subdomain support

## Troubleshooting

### Browser Security Warnings
Since we're using self-signed certificates, browsers will show security warnings. For local development:
- Chrome/Edge: Click "Advanced" → "Proceed to site"
- Firefox: Click "Advanced" → "Accept the Risk and Continue"

### Service Not Starting
Check logs:
```bash
docker-compose logs traefik
docker-compose logs n8n
```

### Hostname Resolution
Ensure your hostname is in `/etc/hosts`:
```bash
127.0.0.1 fairladyz.local n8n.fairladyz.local traefik.fairladyz.local
```

## Next Steps

This setup is ready for:
- Slack OAuth integrations
- Adding databases (PostgreSQL, Redis)
- Adding monitoring services
- Adding development tools
- Scaling with additional N8N instances