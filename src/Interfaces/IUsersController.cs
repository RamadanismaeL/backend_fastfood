using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IUsersController
    {
        Task<ActionResult<IEnumerable<UsersListDTO>>> GetAllAsync();
        Task<IActionResult> CreateAsync([FromBody] UsersCreateDTO user);
        Task<IActionResult> CreateDeftsAsync([FromBody] UsersCreateDeftsDTO user);
        Task<IActionResult> UpdateAsync([FromBody] UsersUpdateDTO user);
        Task<IActionResult> DeleteAsync([FromRoute] Guid id);
    }
}