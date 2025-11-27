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
    public class CashRegisterController(ICashRegisterRepository cashRegisterRep, IHubContext<NotificationHub> hubContext) : ControllerBase, ICashRegisterController
    {
        private readonly ICashRegisterRepository _cashRegisterRep = cashRegisterRep;
        private readonly IHubContext<NotificationHub> _hubContext = hubContext;

        [HttpGet("v1/get-all")]
        public async Task<ActionResult<IEnumerable<CashRegisterListDTO>>> GetAllAsync()
        {
            var result = await _cashRegisterRep.GetAllAsync();
            return Ok(result);
        }

        [HttpPost("v1/open-register")]
        public async Task<IActionResult> OpenRegisterAsync([FromBody] CashRegisterOpenDTO cashRegister)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));

            var result = await _cashRegisterRep.OpenRegisterAsync(cashRegister);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.AlreadyExists ? Conflict(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }

        [HttpPut("v1/close-register")]
        public async Task<IActionResult> CloseRegisterAsync([FromBody] CashRegisterCloseDTO cashRegister)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));

            var result = await _cashRegisterRep.CloseRegisterAsync(cashRegister);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.NotFound ? NotFound(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }

        [HttpDelete("v1/delete/{id:guid}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] Guid id)
        {
            var result = await _cashRegisterRep.DeleteAsync(id);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.NotFound ? NotFound(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }
    }
}