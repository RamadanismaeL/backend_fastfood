using FluentValidation;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Validators
{
    public sealed class UsersCreateDeftsValidator: AbstractValidator<UsersCreateDeftsDTO>
    {
        public UsersCreateDeftsValidator()
        {
            RuleFor(u => u.Username)
                .NotEmpty()
                .WithMessage("Username is required")
                .Length(3, 50)
                .WithMessage("Username must be between 3 and 50 characters");
        }
    }
}