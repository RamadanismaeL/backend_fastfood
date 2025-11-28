using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface ICustomerController
    {
        Task<ActionResult<IEnumerable<CustomerListDTO>>> GetAllAsync();
        Task<IActionResult> CreateAsync([FromBody] CustomerCreateDTO customer);
        Task<IActionResult> UpdateAsync([FromBody] CustomerUpdateDTO customer);
        Task<IActionResult> DeleteAsync([FromRoute] Guid id);
    }
}