using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Data;
using TodoApi.Infrastructure.Repositories;
using Xunit;

namespace TodoApi.Tests.Integration.Repositories
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"UserRepositoryTests-{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new UserRepository(_context);

            SeedData();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
        {
            // Act
            var user = await _repository.GetByIdAsync(1);

            // Assert
            user.Should().NotBeNull();
            user!.Name.Should().Be("Test User");
            user.Email.Should().Be("test@example.com");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Act
            var user = await _repository.GetByIdAsync(999);

            // Assert
            user.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnUser_WhenEmailExists()
        {
            // Act
            var user = await _repository.GetByEmailAsync("test@example.com");

            // Assert
            user.Should().NotBeNull();
            user!.Name.Should().Be("Test User");
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnNull_WhenEmailDoesNotExist()
        {
            // Act
            var user = await _repository.GetByEmailAsync("nonexistent@example.com");

            // Assert
            user.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ShouldPersistUser()
        {
            var user = new User
            {
                Name = "New User",
                Email = "new@example.com",
                PasswordHash = "hashedpassword",
                BiometricToken = "biotoken123",
                Role = "User"
            };

            await _repository.AddAsync(user);

            var stored = await _context.Users.FindAsync(user.Id);
            stored.Should().NotBeNull();
            stored!.Name.Should().Be("New User");
            stored.Email.Should().Be("new@example.com");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyUser()
        {
            var user = await _context.Users.FindAsync(1);
            user!.Name = "Updated Name";

            await _repository.UpdateAsync(user);

            var updated = await _context.Users.FindAsync(1);
            updated!.Name.Should().Be("Updated Name");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveUser()
        {
            var user = await _context.Users.FindAsync(1);

            await _repository.DeleteAsync(user!);

            var exists = await _context.Users.AnyAsync(u => u.Id == 1);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByEmailAsync_ShouldReturnTrue_WhenEmailExists()
        {
            // Act
            var exists = await _repository.ExistsByEmailAsync("test@example.com");

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByEmailAsync_ShouldReturnFalse_WhenEmailDoesNotExist()
        {
            // Act
            var exists = await _repository.ExistsByEmailAsync("nonexistent@example.com");

            // Assert
            exists.Should().BeFalse();
        }

        private void SeedData()
        {
            var user = new User
            {
                Id = 1,
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                BiometricToken = "biotoken123",
                Role = "User"
            };

            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}