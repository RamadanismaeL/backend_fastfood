using FluentValidation;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Validators
{
    public sealed class ProductsCreateValidator : AbstractValidator<ProductsCreateDTO>
    {
        public ProductsCreateValidator()
        {
            RuleFor(i => i.ItemName)
                .NotEmpty()
                .WithMessage("Item Name is required")
                .Length(2, 50)
                .WithMessage("Item Name must be between 2 and 50 characters");
        }
    }
}