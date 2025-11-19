using FluentValidation;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Validators
{
    public sealed class UsersUpdateValidator: AbstractValidator<UsersUpdateDTO>
    {
        public UsersUpdateValidator()
        {
            RuleFor(u => u.Id)
                .NotEmpty()
                .WithMessage("Id is required");

            RuleFor(u => u.Username)
                .NotEmpty()
                .WithMessage("Username is required")
                .Length(3, 50)
                .WithMessage("Username must be between 3 and 50 characters");

            RuleFor(u => u.Roles)
                .NotEmpty()
                .WithMessage("Role is required");
        }
    }
}