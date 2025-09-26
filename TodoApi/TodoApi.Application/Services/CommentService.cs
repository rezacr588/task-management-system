using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Interfaces;

namespace TodoApi.Application.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly ITodoItemRepository _todoItemRepository;
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IMapper _mapper;

        public CommentService(
            ICommentRepository commentRepository,
            ITodoItemRepository todoItemRepository,
            IActivityLogRepository activityLogRepository,
            IMapper mapper)
        {
            _commentRepository = commentRepository;
            _todoItemRepository = todoItemRepository;
            _activityLogRepository = activityLogRepository;
            _mapper = mapper;
        }

        public async Task<CommentDto> CreateCommentAsync(CommentCreateRequest request)
        {
            var todoItem = await _todoItemRepository.GetByIdAsync(request.TodoItemId);
            if (todoItem == null)
            {
                throw new KeyNotFoundException($"Todo item with id {request.TodoItemId} does not exist.");
            }

            var comment = _mapper.Map<Comment>(request);
            comment.AuthorDisplayName ??= todoItem.AssignedToUser?.Name;

            await _commentRepository.AddAsync(comment);

            var activity = new ActivityLogEntry
            {
                TodoItemId = comment.TodoItemId,
                ActorId = request.AuthorId,
                Summary = "Comment added",
                Details = TruncateDetails(comment.Content),
                EventType = ActivityEventType.CommentCreated,
                RelatedCommentId = comment.Id
            };

            await _activityLogRepository.AddAsync(activity);

            return _mapper.Map<CommentDto>(comment);
        }

        public async Task<CommentDto> UpdateCommentAsync(int id, CommentUpdateRequest request)
        {
            var comment = await _commentRepository.GetByIdAsync(id);

            if (!string.IsNullOrWhiteSpace(request.Content))
            {
                comment.Content = request.Content;
            }

            if (request.IsSystemGenerated.HasValue)
            {
                comment.IsSystemGenerated = request.IsSystemGenerated.Value;
            }

            if (request.EventType.HasValue)
            {
                comment.EventType = request.EventType.Value;
            }

            if (request.MetadataJson != null)
            {
                comment.MetadataJson = request.MetadataJson;
            }

            comment.UpdatedAt = DateTime.UtcNow;

            await _commentRepository.UpdateAsync(comment);

            var activity = new ActivityLogEntry
            {
                TodoItemId = comment.TodoItemId,
                ActorId = comment.AuthorId,
                Summary = "Comment updated",
                Details = TruncateDetails(comment.Content),
                EventType = ActivityEventType.CommentUpdated,
                RelatedCommentId = comment.Id
            };

            await _activityLogRepository.AddAsync(activity);

            return _mapper.Map<CommentDto>(comment);
        }

        public async Task DeleteCommentAsync(int id)
        {
            var comment = await _commentRepository.GetByIdAsync(id);
            await _commentRepository.DeleteAsync(comment);

            var activity = new ActivityLogEntry
            {
                TodoItemId = comment.TodoItemId,
                ActorId = comment.AuthorId,
                Summary = "Comment deleted",
                Details = TruncateDetails(comment.Content),
                EventType = ActivityEventType.CommentDeleted,
                RelatedCommentId = comment.Id
            };

            await _activityLogRepository.AddAsync(activity);
        }

        public async Task<CommentDto> GetCommentByIdAsync(int id)
        {
            var comment = await _commentRepository.GetByIdAsync(id);
            return _mapper.Map<CommentDto>(comment);
        }

        public async Task<IEnumerable<CommentDto>> GetCommentsForTodoAsync(int todoItemId)
        {
            var comments = await _commentRepository.GetByTodoItemIdAsync(todoItemId);
            return comments.Select(comment => _mapper.Map<CommentDto>(comment)).ToList();
        }

        private static string TruncateDetails(string content)
        {
            const int maxLength = 400;
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            return content.Length <= maxLength ? content : content.Substring(0, maxLength) + "...";
        }
    }
}
