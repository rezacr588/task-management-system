# Use the official .NET runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["TodoApi/TodoApi.WebApi/TodoApi.WebApi.csproj", "TodoApi/TodoApi.WebApi/"]
COPY ["TodoApi/TodoApi.Application/TodoApi.Application.csproj", "TodoApi/TodoApi.Application/"]
COPY ["TodoApi/TodoApi.Domain/TodoApi.Domain.csproj", "TodoApi/TodoApi.Domain/"]
COPY ["TodoApi/TodoApi.Infrastructure/TodoApi.Infrastructure.csproj", "TodoApi/TodoApi.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "TodoApi/TodoApi.WebApi/TodoApi.WebApi.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/TodoApi/TodoApi.WebApi"
RUN dotnet build "TodoApi.WebApi.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "TodoApi.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app

# Create a non-root user
RUN groupadd -r todoapi && useradd -r -g todoapi todoapi
RUN chown -R todoapi:todoapi /app
USER todoapi

COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TodoApi.WebApi.dll"]