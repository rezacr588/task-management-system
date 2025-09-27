using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Data;
using TodoApi.Infrastructure.Repositories;
using Xunit;

namespace TodoApi.Tests.Integration.Repositories
{
    public class TagRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TagRepository _repository;

        public TagRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TagRepositoryTests-{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new TagRepository(_context);

            SeedData();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnTag_WhenExists()
        {
            // Act
            var tag = await _repository.GetByIdAsync(1);

            // Assert
            tag.Should().NotBeNull();
            tag!.Name.Should().Be("Work");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenDoesNotExist()
        {
            // Act
            var tag = await _repository.GetByIdAsync(999);

            // Assert
            tag.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllTags()
        {
            // Act
            var tags = await _repository.GetAllAsync();

            // Assert
            tags.Should().HaveCount(2);
            tags.Should().Contain(t => t.Name == "Work");
            tags.Should().Contain(t => t.Name == "Personal");
        }

        [Fact]
        public async Task AddAsync_ShouldPersistTag()
        {
            var tag = new Tag
            {
                Name = "New Tag"
            };

            await _repository.AddAsync(tag);

            var stored = await _context.Tags.FindAsync(tag.Id);
            stored.Should().NotBeNull();
            stored!.Name.Should().Be("New Tag");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyTag()
        {
            var tag = await _context.Tags.FindAsync(1);
            tag!.Name = "Updated Work";

            await _repository.UpdateAsync(tag);

            var updated = await _context.Tags.FindAsync(1);
            updated!.Name.Should().Be("Updated Work");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveTag()
        {
            var tag = await _context.Tags.FindAsync(1);

            await _repository.DeleteAsync(tag!);

            var exists = await _context.Tags.AnyAsync(t => t.Id == 1);
            exists.Should().BeFalse();
        }

        private void SeedData()
        {
            var tags = new[]
            {
                new Tag
                {
                    Id = 1,
                    Name = "Work"
                },
                new Tag
                {
                    Id = 2,
                    Name = "Personal"
                }
            };

            _context.Tags.AddRange(tags);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}