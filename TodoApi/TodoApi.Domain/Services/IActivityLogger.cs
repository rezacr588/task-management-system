using System.Collections.Generic;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace TodoApi.Domain.Services
{
    public interface IActivityLogger
    {
        IEnumerable<ActivityLogEntry> GetActivityLogEntries(TodoItem original, TodoItem updated);
        ActivityLogEntry CreateCompletionEntry(TodoItem todoItem, bool isComplete);
    }
}