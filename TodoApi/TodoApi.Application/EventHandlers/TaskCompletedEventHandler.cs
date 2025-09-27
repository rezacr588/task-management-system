using MediatR;
using Microsoft.Extensions.Logging;
using TodoApi.Application.Events;
using TodoApi.Domain.Events;

namespace TodoApi.Application.EventHandlers
{
    public class TaskCompletedEventHandler : INotificationHandler<MediatRDomainEvent>
    {
        private readonly ILogger<TaskCompletedEventHandler> _logger;

        public TaskCompletedEventHandler(ILogger<TaskCompletedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(MediatRDomainEvent notification, CancellationToken cancellationToken)
        {
            if (notification.DomainEvent is TaskCompletedEvent taskCompletedEvent)
            {
                _logger.LogInformation("Task '{Title}' (ID: {Id}) was completed by user {UserId} on {CompletedDate}",
                    taskCompletedEvent.TodoItem.Title,
                    taskCompletedEvent.TodoItem.Id,
                    taskCompletedEvent.TodoItem.AssignedToUserId,
                    taskCompletedEvent.TodoItem.CompletedDate);

                // Here you could add additional business logic like:
                // - Send notifications to stakeholders
                // - Update user statistics
                // - Trigger follow-up tasks
                // - Send emails or push notifications
            }

            return Task.CompletedTask;
        }
    }
}