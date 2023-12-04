using TodoApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext with PostgreSQL Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS services and define the policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAngularApp",
                      policyBuilder =>
                      {
                          policyBuilder.WithOrigins("http://localhost:4200") // Replace with your Angular app's URL
                                       .AllowAnyHeader()
                                       .AllowAnyMethod();
                      });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngularApp");

// Map your API endpoints here

app.Run();
