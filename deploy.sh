#!/bin/bash
set -e

echo "🚀 Todo API Production Deployment Script"
echo "========================================"

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

# Check if .env file exists
if [ ! -f .env ]; then
    echo "⚠️  .env file not found. Creating from template..."
    cp .env.template .env
    echo "📝 Please edit .env file with your production values before running again."
    echo "   Especially update:"
    echo "   - Database connection settings"
    echo "   - JWT signing keys"
    echo "   - Allowed hosts"
    exit 1
fi

echo "🔍 Loading environment variables..."
source .env

# Validate required environment variables
required_vars=("JWT_KEY" "POSTGRES_HOST" "POSTGRES_USER" "POSTGRES_PASSWORD")
for var in "${required_vars[@]}"; do
    if [ -z "${!var}" ]; then
        echo "❌ Required environment variable $var is not set in .env file"
        exit 1
    fi
done

echo "🧪 Building application..."
dotnet build TodoApi/Api.sln --configuration Release

echo "🐳 Building Docker images..."
docker-compose build

echo "🗄️  Setting up database..."
docker-compose up -d postgres

echo "⏰ Waiting for database to be ready..."
sleep 10

echo "🚀 Starting application..."
docker-compose up -d

echo "🔍 Checking application health..."
sleep 5

# Check if application is responding
if curl -f http://localhost:8080/health > /dev/null 2>&1; then
    echo "✅ Application is running successfully!"
    echo ""
    echo "🌐 Application URLs:"
    echo "   - API: http://localhost:8080"
    echo "   - Health Check: http://localhost:8080/health"
    echo ""
    echo "📋 Useful commands:"
    echo "   - View logs: docker-compose logs -f todoapi"
    echo "   - Stop application: docker-compose down"
    echo "   - Update application: docker-compose up -d --build"
else
    echo "❌ Application health check failed. Check logs with:"
    echo "   docker-compose logs todoapi"
    exit 1
fi