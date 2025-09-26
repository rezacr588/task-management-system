using TodoApi.Application.DTOs;

namespace TodoApi.Application.Interfaces
{
    public interface IActivityLogService
    {
        Task<ActivityLogDto> GetActivityByIdAsync(int id);
        Task<IEnumerable<ActivityLogDto>> GetActivityForTodoAsync(int todoItemId);
        Task<ActivityLogDto> RecordAsync(ActivityLogDto activity);
        Task RecordRangeAsync(IEnumerable<ActivityLogDto> activities);
    }
}
