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
    public class IngredientsController(IIngredientsRepository ingredientsRep, IHubContext<NotificationHub> hubContext) : ControllerBase, IIngredientsController
    {
        private readonly IIngredientsRepository _ingredientsRep = ingredientsRep;
        private readonly IHubContext<NotificationHub> _hubContext = hubContext;

        [HttpGet("v1/get-all")]
        public async Task<ActionResult<IEnumerable<IngredientsListDTO>>> GetAllAsync()
        {
            var response = await _ingredientsRep.GetAllAsync();
            return Ok(response);
        }

        [HttpPost("v1/create")]
        public async Task<IActionResult> CreateAsync([FromForm] IngredientsCreateDTO ingredient)
        {
            if (!ModelState.IsValid) return BadRequest(new ResponseDTO { IsSuccess = false, Message = "Invalid data provided." });

            var response = await _ingredientsRep.CreateAsync(ingredient);
            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");

            return response.IsSuccess
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpPatch("v1/update")]
        public async Task<IActionResult> UpdateAsync([FromBody] IngredientsUpdateDTO ingredient)
        {
            if (!ModelState.IsValid) return BadRequest(new ResponseDTO { IsSuccess = false, Message = "Invalid data provided." });

            var response = await _ingredientsRep.UpdateAsync(ingredient);
            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");

            return response.IsSuccess
                ? Ok(response)
                : BadRequest(response);
        }

        [HttpDelete("v1/delete/{id:guid}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] Guid id)
        {
            if (!ModelState.IsValid) return BadRequest(new ResponseDTO { IsSuccess = false, Message = "User not found." });

            var response = await _ingredientsRep.DeleteAsync(id);
            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");

            return response.IsSuccess
                ? Ok(response)
                : BadRequest(response);
        }
    }
}