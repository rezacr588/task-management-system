# Task Management System

## Overview

The Task Management System is a comprehensive solution for managing tasks, users, and tags. It's built using a clean architecture with separate layers for application logic, domain entities, and infrastructure.

## Features

- **Task Management**: Create, update, delete, and retrieve tasks with details like title, description, due date, and completion status.
- **User Management**: Handle user registration, updates, and retrieval. Includes features like biometric token validation and JWT token generation for authentication.
- **Tag Management**: Manage tags associated with tasks, allowing for better organization and categorization of tasks.

## Architecture

- **Application Layer**: Contains DTOs, interfaces, and services for handling business logic.
  - DTOs like `TagDto`, `TodoItemDto`, `UserDto`, `UserRegistrationModel`, and `UserUpdateDto` for data transfer.
  - Interfaces like `IAuthorizationService`, `ITagService`, `ITodoItemService`, and `IUserService` define the contracts for services.
  - Services like `AuthorizationService`, `TodoItemService`, and `UserService` implement the business logic.
- **Domain Layer**: Defines entities and interfaces for the domain model.
  - Entities like `Tag`, `TodoItem`, and `User` represent the core business objects.
  - Interfaces like `IRepository`, `ITodoItemRepository`, and `IUserRepository` for data access.
  - Enums like `PriorityLevel` for defining priority levels of tasks.
- **Infrastructure Layer**: Handles data persistence and other infrastructure concerns.
  - `ApplicationDbContext` for database context.
  - Repositories for implementing data access logic.

## Technologies

- .NET 8.0
- Entity Framework Core
- AutoMapper for object mapping

## Setup and Configuration

- Ensure .NET 8.0 SDK is installed.
- Update `appsettings.json` with the necessary configuration.
- Run the application using `dotnet run`.

## Contributing

Contributions to the Task Management System are welcome. Please follow the standard procedures for submitting issues and pull requests.
