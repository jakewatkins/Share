#!/bin/bash
# EmailServices.Api - Development Docker Helper
# Quick commands for Docker development workflow

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
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

# Check if docker is running
if ! docker info >/dev/null 2>&1; then
    print_error "Docker is not running. Please start Docker Desktop."
    exit 1
fi

# Create logs directory if it doesn't exist
mkdir -p logs

case "$1" in
    "build")
        print_status "Building EmailServices.Api Docker image..."
        docker-compose build emailservices-api
        print_status "Build complete!"
        ;;
    "up")
        print_status "Starting development environment..."
        docker-compose up -d
        print_status "Services started. API available at http://localhost:5000"
        print_status "Swagger UI: http://localhost:5000/swagger"
        print_status "Health check: http://localhost:5000/health"
        ;;
    "down")
        print_status "Stopping development environment..."
        docker-compose down
        print_status "Environment stopped."
        ;;
    "logs")
        print_status "Showing container logs..."
        docker-compose logs -f emailservices-api
        ;;
    "restart")
        print_status "Restarting services..."
        docker-compose restart
        print_status "Services restarted."
        ;;
    "clean")
        print_warning "Cleaning up Docker resources..."
        docker-compose down
        docker system prune -f
        print_status "Cleanup complete."
        ;;
    "test")
        print_status "Running API health check..."
        sleep 2
        curl -f http://localhost:5000/health || print_error "Health check failed!"
        print_status "Testing Swagger endpoint..."
        curl -s http://localhost:5000/swagger/index.html > /dev/null && print_status "Swagger OK" || print_error "Swagger failed!"
        ;;
    *)
        echo "EmailServices.Api Development Docker Helper"
        echo "Usage: $0 {build|up|down|logs|restart|clean|test}"
        echo ""
        echo "Commands:"
        echo "  build    - Build the Docker image"
        echo "  up       - Start development environment"
        echo "  down     - Stop development environment"
        echo "  logs     - Show container logs"
        echo "  restart  - Restart services"
        echo "  clean    - Stop services and clean up Docker resources"
        echo "  test     - Test API endpoints"
        exit 1
        ;;
esac
