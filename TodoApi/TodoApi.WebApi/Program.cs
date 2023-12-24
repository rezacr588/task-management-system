using Microsoft.EntityFrameworkCore;
using TodoApi.Infrastructure.Data; // Adjust this to your actual namespace
using TodoApi.Application.Interfaces; // Adjust this to your actual namespace
using TodoApi.Application.Services; // Adjust this to your actual namespace
using Microsoft.OpenApi.Models;

using TodoApi.Domain.Interfaces;
using TodoApi.Infrastructure.Services;
using TodoApi.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the DI container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to DI container
builder.Services.AddScoped<IUserService,UserService>();
builder.Services.AddScoped<ITodoItemService,TodoItemService>();
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ITokenValidator, BiometricTokenValidator>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITodoItemRepository, TodoItemRepository>();
builder.Services.AddAutoMapper(typeof(IStartup));

// Add other scoped services
builder.Services.AddControllers();

// Add Swagger/OpenAPI support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
