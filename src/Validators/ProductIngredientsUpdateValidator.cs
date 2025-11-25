using FluentValidation;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Validators
{
    public sealed class ProductIngredientsUpdateValidator : AbstractValidator<ProductIngredientsUpdateDTO>
    {
        public ProductIngredientsUpdateValidator()
        {
            RuleFor(pi => pi.Id)
                .NotEmpty()
                .WithMessage("Product is required");

            RuleFor(pi => pi.IngredientId)
                .NotEmpty()
                .WithMessage("Ingredient is required");
        }
    }
}