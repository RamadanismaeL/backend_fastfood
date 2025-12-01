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
    public class CustomersController (ICustomersRepository customerRepository, IHubContext<NotificationHub> hubContext) : ControllerBase, ICustomersController
    {
        private readonly ICustomersRepository _customerRepository = customerRepository;
        private readonly IHubContext<NotificationHub> _hubContext = hubContext;

        [HttpGet("v1/get-all")]
        public async Task<ActionResult<IEnumerable<CustomerListDTO>>> GetAllAsync()
        {
            var result = await _customerRepository.GetAllAsync();
            return Ok(result);
        }

        [HttpPost("v1/create")]
        public async Task<IActionResult> CreateAsync([FromBody] CustomerCreateDTO customer)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));

            var result = await _customerRepository.CreateAsync(customer);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.AlreadyExists ? Conflict(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }

        [HttpPatch("v1/update")]
        public async Task<IActionResult> UpdateAsync([FromBody] CustomerUpdateDTO customer)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));

            var result = await _customerRepository.UpdateAsync(customer);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.NotFound ? NotFound(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }

        [HttpDelete("v1/delete/{id:guid}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] Guid id)
        {
            var result = await _customerRepository.DeleteAsync(id);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.NotFound ? NotFound(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }
    }
}