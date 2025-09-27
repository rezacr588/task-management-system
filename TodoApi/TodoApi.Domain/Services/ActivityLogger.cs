using System;
using System.Collections.Generic;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace TodoApi.Domain.Services
{
    public class ActivityLogger : IActivityLogger
    {
        public IEnumerable<ActivityLogEntry> GetActivityLogEntries(TodoItem original, TodoItem updated)
        {
            var activities = new List<ActivityLogEntry>();

            if (!string.Equals(original.Title, updated.Title, StringComparison.Ordinal))
            {
                activities.Add(new ActivityLogEntry
                {
                    TodoItemId = original.Id,
                    Summary = "Title changed",
                    Details = $"Title updated from '{original.Title}' to '{updated.Title}'",
                    EventType = ActivityEventType.TaskUpdated
                });
            }

            if (!string.Equals(original.Description, updated.Description, StringComparison.Ordinal))
            {
                activities.Add(new ActivityLogEntry
                {
                    TodoItemId = original.Id,
                    Summary = "Description changed",
                    EventType = ActivityEventType.TaskUpdated
                });
            }

            if (original.Priority != updated.Priority)
            {
                activities.Add(new ActivityLogEntry
                {
                    TodoItemId = original.Id,
                    Summary = "Priority changed",
                    Details = $"Priority updated to {updated.Priority}",
                    EventType = ActivityEventType.PriorityChanged
                });
            }

            if (original.DueDate != updated.DueDate)
            {
                activities.Add(new ActivityLogEntry
                {
                    TodoItemId = original.Id,
                    Summary = "Due date changed",
                    Details = $"Due date updated to {updated.DueDate:u}",
                    EventType = ActivityEventType.DueDateChanged
                });
            }

            if (original.AssignedToUserId != updated.AssignedToUserId)
            {
                activities.Add(new ActivityLogEntry
                {
                    TodoItemId = original.Id,
                    Summary = "Assignment changed",
                    Details = $"Assigned user changed to {updated.AssignedToUserId}",
                    EventType = ActivityEventType.AssignmentChanged
                });
            }

            if (original.IsComplete != updated.IsComplete)
            {
                activities.Add(new ActivityLogEntry
                {
                    TodoItemId = original.Id,
                    Summary = updated.IsComplete ? "Task completed" : "Task reopened",
                    EventType = updated.IsComplete ? ActivityEventType.TaskCompleted : ActivityEventType.TaskReopened
                });
            }

            return activities;
        }

        public ActivityLogEntry CreateCompletionEntry(TodoItem todoItem, bool isComplete)
        {
            return new ActivityLogEntry
            {
                TodoItemId = todoItem.Id,
                Summary = isComplete ? "Task completed" : "Task reopened",
                EventType = isComplete ? ActivityEventType.TaskCompleted : ActivityEventType.TaskReopened
            };
        }
    }
}