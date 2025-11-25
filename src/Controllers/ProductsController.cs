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
    public class ProductsController (IProductsRepository productsRepository, IHubContext<NotificationHub> hubContext) : ControllerBase, IProductsController
    {
        private readonly IProductsRepository _productsRep = productsRepository;
        private readonly IHubContext<NotificationHub> _hubContext = hubContext;

        [HttpGet("v1/get-all")]
        public async Task<ActionResult<IEnumerable<ProductsListDTO>>> GetAllAsync()
        {
            var result = await _productsRep.GetAllAsync();
            return Ok(result);
        }

        [HttpPost("v1/create")]
        public async Task<IActionResult> CreateAsync([FromForm] ProductsCreateDTO products)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));

            var result = await _productsRep.CreateAsync(products);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.AlreadyExists ? Conflict(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }

        [HttpPatch("v1/update")]
        public async Task<IActionResult> UpdateAsync([FromForm] ProductsUpdateDTO products)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));

            var result = await _productsRep.UpdateAsync(products);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.NotFound ? NotFound(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }

        [HttpDelete("v1/delete/{id:guid}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] Guid id)
        {
            var result = await _productsRep.DeleteAsync(id);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.NotFound ? NotFound(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }

        [HttpGet("v1/get-product-ingredient/{productId:guid}")]
        public async Task<ActionResult<IEnumerable<ProductIngredientsListDTO>>> GetProductIngredient([FromRoute] Guid productId)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));
            
            var result = await _productsRep.GetProductIngredient(productId);
            return Ok(result);
        }

        [HttpPost("v1/create-product-ingredient")]
        public async Task<IActionResult> CreateProductIngredient([FromBody] ProductIngredientsCreateDTO productIngredient)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));

            var result = await _productsRep.CreateProductIngredient(productIngredient);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.AlreadyExists ? Conflict(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }

        [HttpPatch("v1/update-product-ingredient")]
        public async Task<IActionResult> UpdateProductIngredient([FromBody] ProductIngredientsUpdateDTO productIngredient)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));

            var result = await _productsRep.UpdateProductIngredient(productIngredient);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.NotFound ? NotFound(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }

        [HttpDelete("v1/delete-product-ingredient/{id:guid}")]
        public async Task<IActionResult> DeleteProductIngredient([FromRoute] Guid id)
        {
            var result = await _productsRep.DeleteProductIngredient(id);

            if (!result.IsSuccess)
                return result.Message == MessagesConstant.NotFound ? NotFound(result) : BadRequest(result);

            await _hubContext.Clients.All.SendAsync("keyNotification", "updated");
            return Ok(result);
        }

        [HttpGet("v1/get-select-ingredient")]
        public async Task<ActionResult<IEnumerable<ProductIngredientSelectIngredientDTO>>> GetSelectIngredient()
        {
            var result = await _productsRep.GetSelectIngredient();
            return Ok(result);
        }
    }
}