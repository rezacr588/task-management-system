using System.Collections.Generic;
using System.Threading.Tasks;
using TodoApi.Domain.Entities;

namespace TodoApi.Domain.Interfaces
{
    public interface ITagRepository
    {
        Task<Tag?> GetByIdAsync(int id);
        Task<IEnumerable<Tag>> GetAllAsync();
        Task AddAsync(Tag tag);
        Task UpdateAsync(Tag tag);
        Task DeleteAsync(Tag tag);
        Task<IEnumerable<Tag>> GetTagsForTodoAsync(int todoItemId);
        Task AttachTagToTodoAsync(int todoItemId, int tagId);
        Task DetachTagFromTodoAsync(int todoItemId, int tagId);
    }
}
