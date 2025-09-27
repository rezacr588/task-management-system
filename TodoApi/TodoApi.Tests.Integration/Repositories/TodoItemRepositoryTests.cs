using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Infrastructure.Data;
using TodoApi.Infrastructure.Repositories;
using Xunit;

namespace TodoApi.Tests.Integration.Repositories
{
    public class TodoItemRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TodoItemRepository _repository;

        public TodoItemRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TodoItemRepositoryTests-{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new TodoItemRepository(_context);

            SeedData();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnTodoItem_WhenExists()
        {
            // Act
            var todo = await _repository.GetByIdAsync(1);

            // Assert
            todo.Should().NotBeNull();
            todo!.Title.Should().Be("Test Todo");
            todo.AssignedToUser.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            // Act
            var todo = await _repository.GetByIdAsync(999);

            // Assert
            todo.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllTodoItems()
        {
            // Act
            var todos = await _repository.GetAllAsync();

            // Assert
            todos.Should().HaveCount(2);
            todos.Should().Contain(t => t.Title == "Test Todo");
            todos.Should().Contain(t => t.Title == "Another Todo");
        }

        [Fact]
        public async Task AddAsync_ShouldPersistTodoItem()
        {
            var user = await _context.Users.FindAsync(1);
            var todo = new TodoItem
            {
                Title = "New Todo",
                Description = "New Description",
                AssignedToUserId = 1,
                AssignedToUser = user!,
                Priority = PriorityLevel.High
            };

            await _repository.AddAsync(todo);

            var stored = await _context.TodoItems.FindAsync(todo.Id);
            stored.Should().NotBeNull();
            stored!.Title.Should().Be("New Todo");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyTodoItem()
        {
            var todo = await _context.TodoItems.FindAsync(1);
            todo!.Title = "Updated Title";

            await _repository.UpdateAsync(todo);

            var updated = await _context.TodoItems.FindAsync(1);
            updated!.Title.Should().Be("Updated Title");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveTodoItem()
        {
            var todo = await _context.TodoItems.FindAsync(1);

            await _repository.DeleteAsync(todo!);

            var exists = await _context.TodoItems.AnyAsync(t => t.Id == 1);
            exists.Should().BeFalse();
        }

        private void SeedData()
        {
            var user = new User
            {
                Id = 1,
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = "hash",
                BiometricToken = "token",
                Role = "User"
            };

            var todos = new[]
            {
                new TodoItem
                {
                    Id = 1,
                    Title = "Test Todo",
                    Description = "Test Description",
                    AssignedToUser = user,
                    AssignedToUserId = user.Id,
                    Priority = PriorityLevel.Medium
                },
                new TodoItem
                {
                    Id = 2,
                    Title = "Another Todo",
                    Description = "Another Description",
                    AssignedToUser = user,
                    AssignedToUserId = user.Id,
                    Priority = PriorityLevel.Low
                }
            };

            _context.Users.Add(user);
            _context.TodoItems.AddRange(todos);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}