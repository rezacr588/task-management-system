using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Infrastructure.Data;
using TodoApi.Infrastructure.Repositories;

namespace TodoApi.Tests.Integration.Repositories
{
    public class CommentRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CommentRepository _repository;

        public CommentRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"CommentRepositoryTests-{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new CommentRepository(_context);

            SeedData();
        }

        [Fact]
        public async Task GetByTodoItemIdAsync_ShouldReturnCommentsInDescendingOrder()
        {
            // Act
            var comments = await _repository.GetByTodoItemIdAsync(1);

            // Assert
            comments.Should().HaveCount(2);
            comments.First().Content.Should().Be("Second comment");
        }

        [Fact]
        public async Task AddAsync_ShouldPersistComment()
        {
            var comment = new Comment
            {
                TodoItemId = 1,
                Content = "New comment",
                AuthorId = 1,
                TodoItem = _context.TodoItems.First(),
                AuthorDisplayName = "Seed User",
                EventType = ActivityEventType.CommentCreated
            };

            await _repository.AddAsync(comment);

            var stored = await _context.Comments.FindAsync(comment.Id);
            stored.Should().NotBeNull();
            stored!.Content.Should().Be("New comment");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveComment()
        {
            var comment = await _context.Comments.FirstAsync();

            await _repository.DeleteAsync(comment);

            var exists = await _context.Comments.AnyAsync(c => c.Id == comment.Id);
            exists.Should().BeFalse();
        }

        private void SeedData()
        {
            var user = new User
            {
                Id = 1,
                Name = "Seed User",
                Email = "seed@example.com",
                PasswordHash = "hash",
                BiometricToken = "token",
                Role = "Admin"
            };

            var todo = new TodoItem
            {
                Id = 1,
                Title = "Todo",
                Description = "Description",
                AssignedToUser = user,
                AssignedToUserId = user.Id,
                Priority = PriorityLevel.Medium
            };

            _context.Users.Add(user);
            _context.TodoItems.Add(todo);

            _context.Comments.AddRange(new Comment
            {
                Id = 1,
                TodoItemId = 1,
                TodoItem = todo,
                AuthorId = 1,
                Content = "First comment",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                EventType = ActivityEventType.CommentCreated
            }, new Comment
            {
                Id = 2,
                TodoItemId = 1,
                TodoItem = todo,
                AuthorId = 1,
                Content = "Second comment",
                CreatedAt = DateTime.UtcNow,
                EventType = ActivityEventType.CommentCreated
            });

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
