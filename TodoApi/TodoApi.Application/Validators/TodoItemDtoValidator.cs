using FluentValidation;
using TodoApi.Application.DTOs;

namespace TodoApi.Application.Validators
{
    public class TodoItemDtoValidator : AbstractValidator<TodoItemDto>
    {
        public TodoItemDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .Length(3, 100).WithMessage("Title must be between 3 and 100 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .Length(10, 500).WithMessage("Description must be between 10 and 500 characters.");

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future.")
                .LessThan(DateTime.UtcNow.AddYears(1)).WithMessage("Due date cannot be more than 1 year in the future.");

            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Priority must be a valid priority level.");

            RuleFor(x => x.AssignedToUserId)
                .GreaterThan(0).When(x => x.AssignedToUserId.HasValue)
                .WithMessage("Assigned user ID must be a positive number.");

            RuleFor(x => x.Tags)
                .NotNull().WithMessage("Tags collection cannot be null.")
                .Must(tags => tags.Count <= 10).WithMessage("A task cannot have more than 10 tags.");

            RuleForEach(x => x.Tags)
                .SetValidator(new TagDtoValidator());

            // Business rule: If task is complete, completed date should be set and should be before or equal to now
            When(x => x.IsComplete, () =>
            {
                RuleFor(x => x.CompletedDate)
                    .NotNull().WithMessage("Completed date is required when task is marked as complete.")
                    .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Completed date cannot be in the future.");
            });

            // Business rule: If completed date is set, task should be marked as complete
            When(x => x.CompletedDate.HasValue, () =>
            {
                RuleFor(x => x.IsComplete)
                    .Equal(true).WithMessage("Task must be marked as complete when completed date is set.");
            });
        }
    }
}