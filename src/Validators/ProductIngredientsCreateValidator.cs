using FluentValidation;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Validators
{
    public sealed class ProductIngredientsCreateValidator : AbstractValidator<ProductIngredientsCreateDTO>
    {
        public ProductIngredientsCreateValidator()
        {
            RuleFor(pi => pi.ProductId)
                .NotEmpty()
                .WithMessage("Product is required");

            RuleFor(pi => pi.IngredientId)
                .NotEmpty()
                .WithMessage("Ingredient is required");
        }
    }
}