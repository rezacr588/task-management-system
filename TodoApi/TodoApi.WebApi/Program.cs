using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using TodoApi.Infrastructure.Data; // Adjust this to your actual namespace
using TodoApi.Application.Interfaces; // Adjust this to your actual namespace
using TodoApi.Application.Services; // Adjust this to your actual namespace
using Microsoft.OpenApi.Models;
using TodoApi.Application.DTOs;

using System;
using TodoApi.Domain.Interfaces;
using TodoApi.Domain.Entities;
// Add other necessary using directives

var builder = WebApplication.CreateBuilder(args);

// Add services to the DI container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to DI container
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TodoItemService>();
// Add other scoped services

builder.Services.AddControllers();

// Add Swagger/OpenAPI support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();