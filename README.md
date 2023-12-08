# TodoApi Project

## Overview

TodoApi is an ASP.NET Core Web API designed to manage todo items. It follows Clean Architecture principles and includes features such as user authentication, CRUD operations for todo items, and more.

## Features

- User registration and authentication.
- CRUD operations for todo items.
- Biometric token validation.
- JWT (JSON Web Token) based authorization.
- Clean Architecture: Domain, Application, Infrastructure, and Presentation layers.

## Prerequisites

- .NET 6 SDK
- Visual Studio 2019/2022 or VSCode with C# extension
- SQL Server (for the database)

## Getting Started

1. **Clone the Repository**

   ```
   git clone https://github.com/rezacr588/TodoApi.git
   cd TodoApi
   ```

2. **Set up the Database**

   - Ensure SQL Server is installed and running.
   - Update the connection string in `appsettings.json` in the WebApi project.

3. **Build the Solution**

   ```
   dotnet build TodoApi.sln
   ```

4. **Run the Application**

   - Using the command line:
     ```
     dotnet run --project /TodoApi.WebApi/TodoApi.WebApi.csproj
     ```
   - Or using Visual Studio, set the WebApi project as the startup project and run.

5. **Access the API**

   - The API will be available at `http://localhost:5150` by default.
   - Use Swagger UI to test the API endpoints.

## Usage

Describe how to use the API, including example requests and responses.

## Contributing

If you'd like to contribute, please fork the repository and use a feature branch. Pull requests are warmly welcome.

## Licensing

State the license or say something like: "The code in this project is licensed under MIT license."
