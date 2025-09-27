# AI Coding Agent Instructions for Task Management System

## Architecture Overview

This is a .NET 9 Clean Architecture application with four distinct layers:

- **Domain** (`TodoApi.Domain`): Core entities, interfaces, and business rules
- **Application** (`TodoApi.Application`): Services, DTOs, and business logic
- **Infrastructure** (`TodoApi.Infrastructure`): EF Core repositories and external services
- **WebApi** (`TodoApi.WebApi`): ASP.NET Core controllers and configuration

## Key Patterns & Conventions

### Repository Pattern

- Interfaces defined in `TodoApi.Domain.Interfaces` (e.g., `ITodoItemRepository`)
- Implementations in `TodoApi.Infrastructure.Repositories`
- All repositories inject `ApplicationDbContext` and use async methods
- Include related entities explicitly (e.g., `.Include(t => t.Tags)`)

### Service Layer

- Services in `TodoApi.Application.Services` implement interfaces from `TodoApi.Application.Interfaces`
- Constructor injection of repositories and `IMapper`
- Business logic with activity logging for all mutations
- Return `Task<T>` for async operations, throw `KeyNotFoundException` for missing entities

### DTOs & Mapping

- DTOs defined in `TodoApi.Application.DTOs`
- AutoMapper profiles in `TodoApi.Application.Mappers`
- Map entities to/from DTOs in service methods
- Use PascalCase for all properties

### Controllers

- RESTful endpoints in `TodoApi.WebApi.Controllers`
- Constructor injection of services
- Return `IActionResult` with appropriate HTTP status codes
- Handle exceptions with try/catch, return 404 for `KeyNotFoundException`

### Authentication & Security

- JWT tokens with `ITokenGenerator` and `ITokenValidator` interfaces
- Biometric token validation for enhanced security
- Authorization service for business rule validation

## Development Workflow

### Building & Running

```bash
# Restore dependencies
dotnet restore TodoApi/Api.sln

# Build with warnings as errors
dotnet build TodoApi/Api.sln

# Run API with hot reload
dotnet watch --project TodoApi/TodoApi.WebApi run

# Run in Docker (dev environment)
docker-compose -f docker-compose.dev.yml up -d
```

### Testing

```bash
# Run all tests
dotnet test

# Run E2E tests (requires test database)
docker-compose -f docker-compose.dev.yml up -d postgres-dev
dotnet test TodoApi/TodoApi.Tests.E2E
```

### Database Setup

- Development: PostgreSQL on port 5433 with `todoapi_dev` database
- Production: PostgreSQL on port 5432 with `todoapi` database
- Auto-migration in development via `EnsureCreatedAsync()`

## AI Features

- **Tag Suggestions**: Azure AI Text Analytics integration in `TagSuggestionService`
- Configured in `appsettings.json` with `TextAnalytics:Endpoint` and `ApiKey`
- Falls back to dummy implementation if not configured
- POST `/api/TagSuggestions` with task description returns suggested tags

## Code Style

- 4-space indentation, braces on new lines
- PascalCase for classes/interfaces/methods/properties
- camelCase for parameters/locals/fields
- Async methods end with `Async`
- Expression-bodied members when they improve clarity
- Place new files beside peers (repositories under `Infrastructure/Repositories`)

## Testing Patterns

- xUnit with `Fact` and `Theory` attributes
- FluentAssertions for readable assertions
- E2E tests use `CustomWebApplicationFactory` with real PostgreSQL
- Test data builders for consistent test setup
- Method naming: `MethodName_StateUnderTest_ExpectedResult`

## Deployment

- Multi-stage Dockerfile with non-root user
- Health checks on `/health` endpoint
- Environment-specific configuration (Development/Production)
- Docker Compose for local development and production

## Key Files to Reference

- `AGENTS.md`: Development guidelines and conventions
- `Program.cs`: DI configuration and middleware setup
- `TodoItemService.cs`: Service layer pattern example
- `TodoItemRepository.cs`: Repository implementation pattern
- `TodoItemsController.cs`: Controller pattern with error handling
- `docker-compose.dev.yml`: Development environment setup</content>
  <parameter name="filePath">/Users/rezazeraat/Projects/task-management-system/.github/copilot-instructions.md
