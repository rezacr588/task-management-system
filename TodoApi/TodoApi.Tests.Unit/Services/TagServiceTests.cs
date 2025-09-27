using AutoMapper;
using FluentAssertions;
using Moq;
using TodoApi.Application.DTOs;
using TodoApi.Application.Mappers;
using TodoApi.Application.Services;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Interfaces;
using Xunit;

namespace TodoApi.Tests.Unit.Services
{
    public class TagServiceTests
    {
        private readonly Mock<ITagRepository> _tagRepositoryMock = new();
        private readonly Mock<ITodoItemRepository> _todoItemRepositoryMock = new();
        private readonly IMapper _mapper;
        private readonly TagService _service;

        public TagServiceTests()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new TagProfile());
            });
            _mapper = mapperConfig.CreateMapper();

            _service = new TagService(_tagRepositoryMock.Object, _todoItemRepositoryMock.Object, _mapper);
        }

        [Fact]
        public async Task CreateTagAsync_ShouldAddAndReturnMappedDto()
        {
            // Arrange
            var dto = new TagDto { Name = "Test Tag" };
            Tag? addedTag = null;
            _tagRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Tag>()))
                .Callback<Tag>(tag =>
                {
                    tag.Id = 1;
                    addedTag = tag;
                })
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateTagAsync(dto);

            // Assert
            result.Id.Should().Be(1);
            result.Name.Should().Be("Test Tag");
            addedTag.Should().NotBeNull();
            _tagRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Tag>()), Times.Once);
        }

        [Fact]
        public async Task GetTagByIdAsync_ShouldReturnMappedDto_WhenTagExists()
        {
            // Arrange
            var tag = new Tag { Id = 1, Name = "Existing Tag" };
            _tagRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(tag);

            // Act
            var result = await _service.GetTagByIdAsync(1);

            // Assert
            result.Id.Should().Be(1);
            result.Name.Should().Be("Existing Tag");
        }

        [Fact]
        public async Task GetTagByIdAsync_ShouldThrow_WhenTagNotFound()
        {
            // Arrange
            _tagRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Tag?)null);

            // Act
            Func<Task> act = async () => await _service.GetTagByIdAsync(1);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Tag with id 1 was not found.");
        }

        [Fact]
        public async Task GetAllTagsAsync_ShouldReturnMappedDtos()
        {
            // Arrange
            var tags = new List<Tag>
            {
                new Tag { Id = 1, Name = "Tag1" },
                new Tag { Id = 2, Name = "Tag2" }
            };
            _tagRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(tags);

            // Act
            var result = await _service.GetAllTagsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Tag1");
        }

        [Fact]
        public async Task UpdateTagAsync_ShouldUpdateFields_WhenTagExists()
        {
            // Arrange
            var existingTag = new Tag { Id = 1, Name = "Old Name" };
            var dto = new TagDto { Name = "New Name" };
            _tagRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingTag);
            _tagRepositoryMock.Setup(repo => repo.UpdateAsync(existingTag)).Returns(Task.CompletedTask);

            // Act
            await _service.UpdateTagAsync(1, dto);

            // Assert
            existingTag.Name.Should().Be("New Name");
            _tagRepositoryMock.Verify(repo => repo.UpdateAsync(existingTag), Times.Once);
        }

        [Fact]
        public async Task UpdateTagAsync_ShouldThrow_WhenTagNotFound()
        {
            // Arrange
            _tagRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Tag?)null);

            // Act
            Func<Task> act = async () => await _service.UpdateTagAsync(1, new TagDto());

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Tag with id 1 was not found.");
        }

        [Fact]
        public async Task DeleteTagAsync_ShouldDelete_WhenTagExists()
        {
            // Arrange
            var tag = new Tag { Id = 1, Name = "Tag" };
            _tagRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(tag);
            _tagRepositoryMock.Setup(repo => repo.DeleteAsync(tag)).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteTagAsync(1);

            // Assert
            _tagRepositoryMock.Verify(repo => repo.DeleteAsync(tag), Times.Once);
        }

        [Fact]
        public async Task DeleteTagAsync_ShouldThrow_WhenTagNotFound()
        {
            // Arrange
            _tagRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Tag?)null);

            // Act
            Func<Task> act = async () => await _service.DeleteTagAsync(1);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Tag with id 1 was not found.");
        }

        [Fact]
        public async Task GetTagsForTodoAsync_ShouldReturnTags_WhenTodoExists()
        {
            // Arrange
            var user = new User { Id = 1, Name = "User", Email = "user@example.com", PasswordHash = "hash", BiometricToken = "token", Role = "User" };
            var todo = new TodoItem { Id = 1, Title = "Todo", Description = "Desc", AssignedToUser = user };
            var tags = new List<Tag> { new Tag { Id = 1, Name = "Tag1" } };
            _todoItemRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(todo);
            _tagRepositoryMock.Setup(repo => repo.GetTagsForTodoAsync(1)).ReturnsAsync(tags);

            // Act
            var result = await _service.GetTagsForTodoAsync(1);

            // Assert
            result.Should().HaveCount(1);
            result.First().Name.Should().Be("Tag1");
        }

        [Fact]
        public async Task GetTagsForTodoAsync_ShouldThrow_WhenTodoNotFound()
        {
            // Arrange
            _todoItemRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((TodoItem?)null);

            // Act
            Func<Task> act = async () => await _service.GetTagsForTodoAsync(1);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Todo item with id 1 was not found.");
        }

        [Fact]
        public async Task AttachTagToTodoAsync_ShouldAttach_WhenBothExist()
        {
            // Arrange
            var user = new User { Id = 1, Name = "User", Email = "user@example.com", PasswordHash = "hash", BiometricToken = "token", Role = "User" };
            var todo = new TodoItem { Id = 1, Title = "Todo", Description = "Desc", AssignedToUser = user };
            var tag = new Tag { Id = 1, Name = "Tag" };
            _todoItemRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(todo);
            _tagRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(tag);
            _tagRepositoryMock.Setup(repo => repo.AttachTagToTodoAsync(1, 1)).Returns(Task.CompletedTask);

            // Act
            await _service.AttachTagToTodoAsync(1, 1);

            // Assert
            _tagRepositoryMock.Verify(repo => repo.AttachTagToTodoAsync(1, 1), Times.Once);
        }

        [Fact]
        public async Task AttachTagToTodoAsync_ShouldThrow_WhenTodoNotFound()
        {
            // Arrange
            _todoItemRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((TodoItem?)null);

            // Act
            Func<Task> act = async () => await _service.AttachTagToTodoAsync(1, 1);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Todo item with id 1 was not found.");
        }

        [Fact]
        public async Task AttachTagToTodoAsync_ShouldThrow_WhenTagNotFound()
        {
            // Arrange
            var user = new User { Id = 1, Name = "User", Email = "user@example.com", PasswordHash = "hash", BiometricToken = "token", Role = "User" };
            var todo = new TodoItem { Id = 1, Title = "Todo", Description = "Desc", AssignedToUser = user };
            _todoItemRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(todo);
            _tagRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Tag?)null);

            // Act
            Func<Task> act = async () => await _service.AttachTagToTodoAsync(1, 1);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Tag with id 1 was not found.");
        }

        [Fact]
        public async Task DetachTagFromTodoAsync_ShouldDetach()
        {
            // Arrange
            _tagRepositoryMock.Setup(repo => repo.DetachTagFromTodoAsync(1, 1)).Returns(Task.CompletedTask);

            // Act
            await _service.DetachTagFromTodoAsync(1, 1);

            // Assert
            _tagRepositoryMock.Verify(repo => repo.DetachTagFromTodoAsync(1, 1), Times.Once);
        }
    }
}