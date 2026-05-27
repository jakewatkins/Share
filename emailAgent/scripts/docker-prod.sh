#!/bin/bash
# EmailServices.Api - Production Docker Helper
# Production deployment and management commands

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo -e "${BLUE}[PROD]${NC} $1"
}

# Check if docker is running
if ! docker info >/dev/null 2>&1; then
    print_error "Docker is not running. Please start Docker."
    exit 1
fi

# Create data directory for production logs
mkdir -p data/logs

# Ensure production settings exist
if [ ! -f "settings.production.json" ]; then
    print_error "Production settings file not found: settings.production.json"
    exit 1
fi

case "$1" in
    "deploy")
        print_header "Deploying EmailServices.Api to production..."
        docker-compose -f docker-compose.prod.yml build emailservices-api
        docker-compose -f docker-compose.prod.yml up -d emailservices-api
        print_status "Production deployment complete!"
        print_status "API available at http://localhost:80"
        print_status "Health check: http://localhost:80/health"
        ;;
    "status")
        print_header "Production environment status:"
        docker-compose -f docker-compose.prod.yml ps
        ;;
    "logs")
        print_header "Production logs:"
        docker-compose -f docker-compose.prod.yml logs -f emailservices-api
        ;;
    "stop")
        print_warning "Stopping production environment..."
        docker-compose -f docker-compose.prod.yml down
        print_status "Production environment stopped."
        ;;
    "update")
        print_header "Updating production deployment..."
        docker-compose -f docker-compose.prod.yml pull emailservices-api
        docker-compose -f docker-compose.prod.yml up -d emailservices-api
        print_status "Update complete."
        ;;
    "health")
        print_header "Checking production health..."
        curl -f http://localhost:80/health && print_status "API is healthy" || print_error "API health check failed!"
        ;;
    "backup")
        print_header "Creating production backup..."
        BACKUP_DIR="backup/$(date +%Y%m%d_%H%M%S)"
        mkdir -p "$BACKUP_DIR"
        cp -r data/logs "$BACKUP_DIR/"
        cp settings.production.json "$BACKUP_DIR/"
        print_status "Backup created in $BACKUP_DIR"
        ;;
    "proxy")
        print_header "Starting with reverse proxy..."
        docker-compose -f docker-compose.prod.yml --profile proxy up -d
        print_status "Production with proxy started!"
        print_status "API available via proxy at http://localhost:8080"
        print_status "Traefik dashboard: http://localhost:8081"
        ;;
    *)
        echo "EmailServices.Api Production Docker Helper"
        echo "Usage: $0 {deploy|status|logs|stop|update|health|backup|proxy}"
        echo ""
        echo "Commands:"
        echo "  deploy   - Deploy to production environment"
        echo "  status   - Show production environment status"
        echo "  logs     - Show production logs"
        echo "  stop     - Stop production environment"
        echo "  update   - Pull and restart with latest image"
        echo "  health   - Check API health"
        echo "  backup   - Create backup of production data"
        echo "  proxy    - Start with reverse proxy (Traefik)"
        exit 1
        ;;
esac
