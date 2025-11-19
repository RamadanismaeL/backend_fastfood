using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IUsersController
    {
        Task<ActionResult<IEnumerable<UsersListDTO>>> GetAllAsync();
        Task<IActionResult> CreateAsync([FromForm] UsersCreateDTO user);
        Task<IActionResult> CreateDeftsAsync([FromForm] UsersCreateDeftsDTO user);
        Task<IActionResult> UpdateAsync([FromBody] UsersUpdateDTO user);
        Task<IActionResult> DeleteAsync([FromRoute] Guid id);
    }
}