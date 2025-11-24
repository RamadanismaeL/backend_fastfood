using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IProductsController
    {
        Task<ActionResult<IEnumerable<ProductsListDTO>>> GetAllAsync();
        Task<IActionResult> CreateAsync([FromBody] ProductsCreateDTO products);
        Task<IActionResult> UpdateAsync([FromBody] ProductsUpdateDTO products);
        Task<IActionResult> DeleteAsync([FromRoute] Guid id);
    }
}