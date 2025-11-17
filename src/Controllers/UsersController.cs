using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;

namespace unipos_basic_backend.src.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class UsersController (IUsersRepository usersRepository) : ControllerBase, IUsersController
    {
        private readonly IUsersRepository _usersRepository = usersRepository;

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