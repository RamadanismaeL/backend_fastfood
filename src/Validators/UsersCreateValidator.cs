using FluentValidation;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Validators
{
    public sealed class UsersCreateValidator : AbstractValidator<UsersCreateDTO>
    {
        public UsersCreateValidator()
        {
            RuleFor(u => u.Username)
                .NotEmpty()
                .WithMessage("Username is required")
                .Length(3, 50)
                .WithMessage("Username must be between 3 and 50 characters");

            RuleFor(u => u.Roles)
                .NotEmpty()
                .WithMessage("Role is required");

            RuleFor(u => u.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .Length(6, 100)
                .WithMessage("Password must be at least 6 characters");
        }
    }
}