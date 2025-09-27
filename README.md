# Todo API - Production-Ready Task Management System

## Overview

A production-ready Todo API built with .NET 9.0, featuring comprehensive task management, user authentication, and a robust architecture. The system supports full CRUD operations for todos, users, tags, and comments with activity logging.

## 🚀 Features

- **Task Management**: Create, update, delete, and retrieve todos with rich metadata
- **User Authentication**: JWT-based authentication with biometric token validation
- **Tag System**: Organize todos with tags and associations
- **Comment System**: Add comments to todos for collaboration
- **Activity Logging**: Track all changes and user activities
- **Health Checks**: Built-in health monitoring endpoints
- **Production Ready**: Docker support, proper logging, and security configurations

## 🏗️ Architecture

The application follows Clean Architecture principles with clear separation of concerns:

- **Domain Layer** (`TodoApi.Domain`): Core business entities and interfaces
- **Application Layer** (`TodoApi.Application`): Business logic, DTOs, and service interfaces
- **Infrastructure Layer** (`TodoApi.Infrastructure`): Data access, external services
- **Web API Layer** (`TodoApi.WebApi`): REST API endpoints and configuration

## 🛠️ Technologies

- **.NET 9.0** - Latest .NET framework
- **Entity Framework Core** - ORM with PostgreSQL
- **AutoMapper** - Object-to-object mapping
- **JWT Authentication** - Secure token-based authentication
- **Swagger/OpenAPI** - API documentation
- **Docker** - Containerization
- **Health Checks** - Application monitoring
- **xUnit** - Testing framework with E2E tests

## 🚦 Getting Started

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL 15+
- Docker & Docker Compose (optional)

### Local Development Setup

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd task-management-system
   ```

2. **Configure the database**

   - Install PostgreSQL locally
   - Create databases: `todoapi_dev` and `todoapi_test`
   - Update connection strings in `appsettings.Development.json`

3. **Install dependencies**

   ```bash
   dotnet restore TodoApi/Api.sln
   ```

4. **Run the application**

   ```bash
   dotnet run --project TodoApi/TodoApi.WebApi
   ```

5. **Access the API**
   - API: `http://localhost:5000`
   - Swagger UI: `http://localhost:5000` (development only)
   - Health Check: `http://localhost:5000/health`

### Docker Development

1. **Start with Docker Compose**

   ```bash
   docker-compose -f docker-compose.dev.yml up -d
   ```

2. **Access the containerized API**
   - API: `http://localhost:5000`
   - PostgreSQL: `localhost:5433`

## 🏭 Production Deployment

### Environment Configuration

1. **Copy environment template**

   ```bash
   cp .env.template .env
   ```

2. **Configure production variables in `.env`**
   ```env
   ConnectionStrings__DefaultConnection=Host=your-db-host;Port=5432;Database=todoapi;Username=user;Password=password
   JWT_KEY=your-super-secret-32-character-key
   JWT_ISSUER=TodoApi.Production
   JWT_AUDIENCE=TodoApi.Production
   ASPNETCORE_ENVIRONMENT=Production
   ```

### Docker Production Deployment

```bash
# Build and deploy
docker-compose up -d

# View logs
docker-compose logs -f todoapi

# Scale the application
docker-compose up -d --scale todoapi=3
```

### Manual Production Setup

1. **Build the application**

   ```bash
   dotnet publish TodoApi/TodoApi.WebApi -c Release -o ./publish
   ```

2. **Configure environment variables**

   ```bash
   export ConnectionStrings__DefaultConnection="your-connection-string"
   export JWT_KEY="your-jwt-key"
   export ASPNETCORE_ENVIRONMENT="Production"
   ```

3. **Run the application**
   ```bash
   cd publish
   dotnet TodoApi.WebApi.dll
   ```

## 🧪 Testing

### Running E2E Tests

The application includes comprehensive E2E tests that use a real PostgreSQL test database:

```bash
# Ensure test database is running
docker-compose -f docker-compose.dev.yml up -d postgres-dev

# Run E2E tests
dotnet test TodoApi/TodoApi.Tests.E2E
```

### Test Coverage

- **Users Endpoints**: Registration, authentication, profile management
- **Todo Items**: CRUD operations, validation, activity tracking
- **Tags**: Tag management and todo associations
- **Comments**: Comment CRUD on todos
- **Activity Log**: Activity tracking verification

## 📊 Monitoring & Health

### Health Checks

- **Endpoint**: `/health`
- **Database**: PostgreSQL connectivity check
- **Response**: JSON health status

### Logging

The application uses structured logging with different levels:

- **Development**: Debug level with SQL query logging
- **Production**: Information level with security-focused logging

## 🔒 Security Considerations

- **JWT Authentication**: Secure token-based authentication
- **Environment Variables**: Sensitive data stored in environment variables
- **HTTPS Redirection**: Enforced in production
- **Connection String Security**: No hardcoded credentials
- **Docker Security**: Non-root user in containers

## 📋 API Endpoints

### Users

- `POST /api/users/register` - Register new user
- `PUT /api/users/{id}` - Update user profile
- `GET /api/users/{id}` - Get user details

### Todos

- `GET /api/todoitems` - List todos
- `POST /api/todoitems` - Create todo
- `GET /api/todoitems/{id}` - Get todo details
- `PUT /api/todoitems/{id}` - Update todo
- `DELETE /api/todoitems/{id}` - Delete todo

### Tags

- `GET /api/tags` - List tags
- `POST /api/tags` - Create tag
- `POST /api/tags/{tagId}/attach/{todoId}` - Attach tag to todo

### Comments

- `GET /api/todoitems/{todoId}/comments` - Get comments
- `POST /api/todoitems/{todoId}/comments` - Add comment

### Activity Log

- `GET /api/activitylog` - Get activity history

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.
