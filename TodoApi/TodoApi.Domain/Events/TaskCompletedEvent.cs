using TodoApi.Domain.Entities;

namespace TodoApi.Domain.Events
{
    public class TaskCompletedEvent : DomainEvent
    {
        public TodoItem TodoItem { get; }

        public TaskCompletedEvent(TodoItem todoItem)
        {
            TodoItem = todoItem;
        }
    }
}