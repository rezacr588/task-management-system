using FluentValidation;
using TodoApi.Application.DTOs;

namespace TodoApi.Application.Validators
{
    public class TagDtoValidator : AbstractValidator<TagDto>
    {
        public TagDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tag name is required.")
                .Length(2, 30).WithMessage("Tag name must be between 2 and 30 characters.")
                .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Tag name can only contain letters, numbers, spaces, hyphens, and underscores.");
        }
    }
}