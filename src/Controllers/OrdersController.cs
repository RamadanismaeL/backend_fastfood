using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using unipos_basic_backend.src.Constants;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;
using unipos_basic_backend.src.Repositories;

namespace unipos_basic_backend.src.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController(IOrdersRepository ordersRepository, IHubContext<NotificationHub> hubContext) : ControllerBase, IOrdersController
    {
        private readonly IOrdersRepository _ordersRepository = ordersRepository;
        private readonly IHubContext<NotificationHub> _hubContext = hubContext;

        [HttpGet("v1/get-all")]
        public async Task<ActionResult<IEnumerable<OrdersListDTO>>> GetAllAsync()
        {
            var result = await _ordersRepository.GetAllAsync();
            return Ok(result);
        }

        [HttpPost("v1/create")]
        public async Task<IActionResult> CreateAsync([FromBody] OrdersCreateDTO order)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));

            var result = await _ordersRepository.CreateAsync(order);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.AlreadyExists ? Conflict(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }
    }
}