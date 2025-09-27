using FluentValidation;
using TodoApi.Application.DTOs;

namespace TodoApi.Application.Validators
{
    public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
    {
        public UserUpdateDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("User ID must be a positive number.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .Length(2, 100).WithMessage("Name must be between 2 and 100 characters.")
                .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Name can only contain letters, spaces, hyphens, apostrophes, and periods.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Please provide a valid email address.")
                .MaximumLength(254).WithMessage("Email address is too long.");

            RuleFor(x => x.Password)
                .MinimumLength(8).When(x => !string.IsNullOrEmpty(x.Password))
                .WithMessage("Password must be at least 8 characters long if provided.")
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.")
                .Matches(@"[A-Z]").When(x => !string.IsNullOrEmpty(x.Password))
                .WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").When(x => !string.IsNullOrEmpty(x.Password))
                .WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").When(x => !string.IsNullOrEmpty(x.Password))
                .WithMessage("Password must contain at least one number.")
                .Matches(@"[^a-zA-Z0-9]").When(x => !string.IsNullOrEmpty(x.Password))
                .WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.BiometricToken)
                .NotEmpty().WithMessage("Biometric token is required.")
                .Length(10, 500).WithMessage("Biometric token must be between 10 and 500 characters.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required.")
                .Must(role => role == "User" || role == "Admin" || role == "Manager")
                .WithMessage("Role must be one of: User, Admin, Manager.");
        }
    }
}