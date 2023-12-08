TodoApi/
│
├──TodoApi.Domain/
│ ├── Entities/
│ │ └── TodoItem.cs
│ ├── Enums/
│ │ └── PriorityLevel.cs
│ └── Interfaces/
│ └── IRepository.cs
│
├──TodoApi.Application/
│ ├── Services/
│ │ ├── TodoItemService.cs
│ │ └── UserService.cs
│ ├── Interfaces/
│ │ ├── ITodoItemService.cs
│ │ └── IUserService.cs
│ ├── DTOs/
│ │ └── TodoItemDto.cs
│ └── Mappers/
│ └──--- TodoItemProfile.cs // For AutoMapper, if used
│
├──TodoApi.Infrastructure/
│ ├── Data/
│ │ ├── ApplicationDbContext.cs
│ │ └── Configurations/
│ ├── Repositories/
│ │ ├── TodoItemRepository.cs
│ │ └── UserRepository.cs
│ └── Logging/
│ └──--- FileLogger.cs // Example for custom logging
│
├──TodoApi.WebApi/
│ ├── Controllers/
│ │ ├── TodoItemsController.cs
│ │ └── UsersController.cs
│ ├── Program.cs
│ └── appsettings.json
│
└── YourProject.sln // Solution file
