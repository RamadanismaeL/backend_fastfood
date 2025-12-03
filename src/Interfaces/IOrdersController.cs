using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IOrdersController
    {
        Task<ActionResult<OrdersCheckPosDTO>> CheckPos([FromRoute] Guid userId);
        Task<ActionResult<IEnumerable<OrdersListDTO>>> GetAllAsync([FromRoute] Guid registerId);
        Task<ActionResult<IEnumerable<OrdersListDTO>>> GetAllAsync();
        Task<IActionResult> CreateAsync([FromBody] OrdersCreateDTO order);
        Task<IActionResult> CreatePayNow([FromBody] OrdersCreatePayNowDTO order);
        Task<ActionResult<string>> GetReceiptNumber();
        Task<IActionResult> UpdatePayNow(OrdersUpdatePayNowDTO order);
    }
}