using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IIngredientsController
    {
        Task<ActionResult<IEnumerable<IngredientsListDTO>>> GetAllAsync();
        Task<IActionResult> CreateAsync([FromForm] IngredientsCreateDTO ingredient);
        Task<IActionResult> UpdateAsync([FromForm] IngredientsUpdateDTO ingredient);
        Task<IActionResult> DeleteAsync([FromRoute] Guid id);
        Task<ActionResult<IngredientsCardsDTO>> GetCardAsync();
    }
}