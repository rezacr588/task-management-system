using FluentValidation;
using TodoApi.Application.DTOs;

namespace TodoApi.Application.Validators
{
    public class CommentCreateRequestValidator : AbstractValidator<CommentCreateRequest>
    {
        public CommentCreateRequestValidator()
        {
            RuleFor(x => x.TodoItemId)
                .GreaterThan(0).WithMessage("Todo item ID must be a positive number.");

            RuleFor(x => x.AuthorId)
                .GreaterThan(0).When(x => x.AuthorId.HasValue)
                .WithMessage("Author ID must be a positive number if provided.");

            RuleFor(x => x.AuthorDisplayName)
                .MaximumLength(100).WithMessage("Author display name cannot exceed 100 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.AuthorDisplayName));

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment content is required.")
                .MaximumLength(4000).WithMessage("Comment content cannot exceed 4000 characters.")
                .MinimumLength(1).WithMessage("Comment content cannot be empty.");

            RuleFor(x => x.MetadataJson)
                .MaximumLength(5000).WithMessage("Metadata JSON cannot exceed 5000 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.MetadataJson));

            // Business rule: If not system generated, author info is required
            When(x => !x.IsSystemGenerated, () =>
            {
                RuleFor(x => x.AuthorId)
                    .NotNull().WithMessage("Author ID is required for non-system generated comments.");

                RuleFor(x => x.AuthorDisplayName)
                    .NotEmpty().WithMessage("Author display name is required for non-system generated comments.");
            });
        }
    }
}