using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IOrdersController
    {
        Task<ActionResult<IEnumerable<OrdersListDTO>>> GetAllAsync();
        Task<IActionResult> CreateAsync([FromBody] OrdersCreateDTO order);
        Task<IActionResult> CreatePayNow([FromBody] OrdersCreatePayNowDTO orderPayNow);
        Task<ActionResult<string>> GetReceiptNumber();
        Task<IActionResult> UpdateAsync(OrdersUpdatePayNowDTO order);
    }
}