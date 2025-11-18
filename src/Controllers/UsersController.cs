using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;
using unipos_basic_backend.src.Repositories;

namespace unipos_basic_backend.src.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class UsersController (IUsersRepository usersRepository, IHubContext<NotificationHub> hubContext) : ControllerBase, IUsersController
    {
        private readonly IUsersRepository _usersRepository = usersRepository;
        private readonly IHubContext<NotificationHub> _hubContext = hubContext;

        [HttpGet("v1/get-all")]
        public async Task<ActionResult<IEnumerable<UsersListDTO>>> GetAllAsync()
        {
            var response = await _usersRepository.GetAllAsync();
            return Ok(response);
        }

        [HttpPost("v1/create")]
        public async Task<IActionResult> CreateAsync([FromForm] UsersCreateDTO user)
        {
            if (!ModelState.IsValid) return BadRequest(new ResponseDTO { IsSuccess = false, Message = "Invalid data provided." });

            var response = await _usersRepository.CreateAsync(user);

            return response.IsSuccess
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpPost("v1/defts-create")]
        public async Task<IActionResult> CreateDeftsAsync([FromBody] UsersCreateDeftsDTO user)
        {
            if (!ModelState.IsValid) return BadRequest(new ResponseDTO { IsSuccess = false, Message = "Invalid data provided." });

            var response = await _usersRepository.CreateDeftsAsync(user);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");

            return response.IsSuccess
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpDelete("v1/delete/{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            if (!ModelState.IsValid) return BadRequest(new ResponseDTO { IsSuccess = false, Message = "User not found." });

            var response = await _usersRepository.DeleteAsync(id);

            return response.IsSuccess
                ? Ok(response)
                : BadRequest(response);
        }
    }
}