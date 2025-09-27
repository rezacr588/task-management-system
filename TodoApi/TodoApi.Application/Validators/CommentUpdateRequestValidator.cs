using FluentValidation;
using TodoApi.Application.DTOs;

namespace TodoApi.Application.Validators
{
    public class CommentUpdateRequestValidator : AbstractValidator<CommentUpdateRequest>
    {
        public CommentUpdateRequestValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment content is required.")
                .MaximumLength(4000).WithMessage("Comment content cannot exceed 4000 characters.")
                .MinimumLength(1).WithMessage("Comment content cannot be empty.");

            RuleFor(x => x.MetadataJson)
                .MaximumLength(5000).WithMessage("Metadata JSON cannot exceed 5000 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.MetadataJson));

            // Business rule: System generated flag and event type should only be set by the system
            RuleFor(x => x.IsSystemGenerated)
                .Null().WithMessage("System generated flag cannot be updated through this endpoint.");

            RuleFor(x => x.EventType)
                .Null().WithMessage("Event type cannot be updated through this endpoint.");
        }
    }
}