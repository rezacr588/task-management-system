#!/bin/bash
set -e

echo "🛠️ Todo API Development Setup"
echo "============================="

# Check prerequisites
echo "🔍 Checking prerequisites..."

if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 9.0 SDK is not installed. Please install it first."
    exit 1
fi

if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

echo "✅ Prerequisites check passed"

echo "🗄️ Starting development database..."
docker-compose -f docker-compose.dev.yml up -d postgres-dev

echo "⏰ Waiting for database to be ready..."
sleep 10

echo "📦 Restoring NuGet packages..."
dotnet restore TodoApi/Api.sln

echo "🏗️ Building solution..."
dotnet build TodoApi/Api.sln

echo "🌱 Setting up development database..."
cd TodoApi/TodoApi.WebApi
ASPNETCORE_ENVIRONMENT=Development dotnet run &
SERVER_PID=$!
sleep 5
kill $SERVER_PID
cd ../..

echo "✅ Development environment is ready!"
echo ""
echo "🚀 To start the application:"
echo "   dotnet run --project TodoApi/TodoApi.WebApi"
echo ""
echo "🧪 To run tests:"
echo "   dotnet test TodoApi/Api.sln"
echo ""
echo "🐳 To use Docker for development:"
echo "   docker-compose -f docker-compose.dev.yml up -d"
echo ""
echo "📋 Development URLs:"
echo "   - API: http://localhost:5000"
echo "   - Swagger UI: http://localhost:5000"
echo "   - Database: localhost:5433"