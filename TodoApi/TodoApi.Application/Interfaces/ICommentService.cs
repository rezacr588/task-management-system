using TodoApi.Application.DTOs;

namespace TodoApi.Application.Interfaces
{
    public interface ICommentService
    {
        Task<CommentDto> CreateCommentAsync(CommentCreateRequest request);
        Task<CommentDto> UpdateCommentAsync(int id, CommentUpdateRequest request);
        Task DeleteCommentAsync(int id);
        Task<CommentDto> GetCommentByIdAsync(int id);
        Task<IEnumerable<CommentDto>> GetCommentsForTodoAsync(int todoItemId);
    }
}
