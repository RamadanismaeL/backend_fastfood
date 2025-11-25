using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IProductsController
    {
        Task<ActionResult<IEnumerable<ProductsListDTO>>> GetAllAsync();
        Task<IActionResult> CreateAsync([FromForm] ProductsCreateDTO products);
        Task<IActionResult> UpdateAsync([FromForm] ProductsUpdateDTO products);
        Task<IActionResult> DeleteAsync([FromRoute] Guid id);
        
        Task<ActionResult<IEnumerable<ProductIngredientsListDTO>>> GetProductIngredient([FromRoute] Guid productId);
        Task<IActionResult> CreateProductIngredient([FromBody] ProductIngredientsCreateDTO productIngredient);
        Task<IActionResult> UpdateProductIngredient([FromBody] ProductIngredientsUpdateDTO productIngredient);
        Task<IActionResult> DeleteProductIngredient([FromRoute] Guid id);
        Task<ActionResult<IEnumerable<ProductIngredientSelectIngredientDTO>>> GetSelectIngredient();
    }
}