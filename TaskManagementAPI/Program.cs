var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS services and define the policy
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAngularApp",
                      policyBuilder =>
                      {
                          policyBuilder.WithOrigins("http://localhost:4200") // Replace with the actual URL of your Angular app
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

// Use CORS with the policy defined above
app.UseCors("AllowAngularApp");

// Map your API endpoints here

app.Run();

// OOP Concept: Record type for TaskItem, demonstrating encapsulation and immutability
record TaskItem(int Id, string Title, string Description, bool IsComplete, DateOnly DueDate);
