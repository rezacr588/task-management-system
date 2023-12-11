task-management-system/
│
├── .github/
│ └── workflows/
│ └── dotnet.yml
│
├── TodoApi/
│ ├── TodoApi.Application/
│ │ ├── DTOs/
│ │ │ ├── TagDto.cs
│ │ │ ├── TodoItemDto.cs
│ │ │ ├── UserRegistrationModel.cs
│ │ │ └── UserUpdateDto.cs
│ │ ├── Interfaces/
│ │ │ ├── IAuthorizationService.cs
│ │ │ ├── ITagService.cs
│ │ │ ├── ITodoItemService.cs
│ │ │ └── IUserService.cs
│ │ ├── Mappers/
│ │ │ └── TodoItemProfile.cs
│ │ ├── Services/
│ │ │ ├── AuthorizationService.cs
│ │ │ ├── TodoItemService.cs
│ │ │ └── UserService.cs
│ │ └── (other application files and directories)
│ │
│ ├── TodoApi.Domain/
│ │ ├── Entities/
│ │ │ ├── Tag.cs
│ │ │ ├── TodoItem.cs
│ │ │ └── User.cs
│ │ ├── Enums/
│ │ │ └── PriorityLevel.cs
│ │ ├── Interfaces/
│ │ │ ├── IRepository.cs
│ │ │ ├── ITodoItemRepository.cs
│ │ │ └── IUserRepository.cs
│ │ └── (other domain files and directories)
│ │
│ ├── TodoApi.Infrastructure/
│ │ ├── Data/
│ │ │ ├── ApplicationDbContext.cs
│ │ │ ├── Configurations/
│ │ │ │ ├── TagConfiguration.cs
│ │ │ │ ├── TodoItemConfiguration.cs
│ │ │ │ └── UserConfiguration.cs
│ │ ├── Logging/
│ │ │ └── FileLogger.cs
│ │ ├── Repositories/
│ │ │ ├── TodoItemRepository.cs
│ │ │ └── UserRepository.cs
│ │ └── (other infrastructure files and directories)
│ │
│ └── TodoApi.WebApi/
│ ├── Controllers/
│ │ ├── TagController.cs
│ │ ├── TodoItemsController.cs
│ │ └── UsersController.cs
│ ├── Properties/
│ │ └── launchSettings.json
│ ├── (other WebApi files and directories)
│ ├── appsettings.json
│ ├── appsettings.Development.json
│ └── TodoApi.WebApi.http
│
├── README.md
└── new-architect.md

```

```
