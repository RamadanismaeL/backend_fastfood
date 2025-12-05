using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.Constants;
using unipos_basic_backend.src.DTOs;
using unipos_basic_backend.src.Interfaces;

namespace unipos_basic_backend.src.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController (IReportsRepository reportsRep) : ControllerBase, IReportsController
    {
        private readonly IReportsRepository _reportsRep = reportsRep;

        [HttpPost("v1/get-cash-movements")]
        public async Task<ActionResult<IEnumerable<ReportsCashMovementsDTO>>> GetCashMovementsAsync([FromBody] DateDTO date)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));
            
            var result = await _reportsRep.GetCashMovementsAsync(date);
            return Ok(result);
        }

        [HttpPost("v1/get-orders-detail")]
        public async Task<ActionResult<IEnumerable<OrdersListDTO>>> GetOrdersDetailAsync([FromBody] DateDTO date)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));
            
            var result = await _reportsRep.GetOrdersDetailAsync(date);
            return Ok(result);
        }
    }
}