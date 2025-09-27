using System.Collections.Generic;
using System.Threading.Tasks;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Interfaces
{
    public interface IActivityLogRepository
    {
        Task<ActivityLogEntry> GetByIdAsync(int id);
        Task<IEnumerable<ActivityLogEntry>> GetByTodoItemIdAsync(int todoItemId);
        Task AddAsync(ActivityLogEntry entry);
        Task AddRangeAsync(IEnumerable<ActivityLogEntry> entries);
    }
}