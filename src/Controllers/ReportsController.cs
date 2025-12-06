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

        [HttpPost("v1/get-initial-balance")]
        public async Task<ActionResult<IEnumerable<ReportsCardDTO>>> GetInitialBalance([FromBody] DateDTO date)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));
            
            var result = await _reportsRep.GetInitialBalance(date);
            return Ok(result);
        }

        [HttpPost("v1/get-inflows")]
        public async Task<ActionResult<IEnumerable<ReportsCardDTO>>> GetInFlows([FromBody] DateDTO date)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));
            
            var result = await _reportsRep.GetInFlows(date);
            return Ok(result);
        }

        [HttpPost("v1/get-outflows")]
        public async Task<ActionResult<IEnumerable<ReportsCardDTO>>> GetOutFlows([FromBody] DateDTO date)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));
            
            var result = await _reportsRep.GetOutFlows(date);
            return Ok(result);
        }

        [HttpPost("v1/get-closing-balance")]
        public async Task<ActionResult<IEnumerable<ReportsCardDTO>>> GetClosingBalance([FromBody] DateDTO date)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));
            
            var result = await _reportsRep.GetClosingBalance(date);
            return Ok(result);
        }

        [HttpPost("v1/get-num-of-sales")]
        public async Task<ActionResult<IEnumerable<ReportsCardNumDTO>>> GetNumOfSales([FromBody] DateDTO date)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));
            
            var result = await _reportsRep.GetNumOfSales(date);
            return Ok(result);
        }

        [HttpPost("v1/get-expected-balance")]
        public async Task<ActionResult<IEnumerable<ReportsCardDTO>>> GetExpectedBalance([FromBody] DateDTO date)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));
            
            var result = await _reportsRep.GetExpectedBalance(date);
            return Ok(result);
        }

        [HttpPost("v1/get-average-ticket")]
        public async Task<ActionResult<IEnumerable<ReportsCardDTO>>> GetAverageTicket([FromBody] DateDTO date)
        {
            if (!ModelState.IsValid) return BadRequest(ResponseDTO.Failure(MessagesConstant.InvalidData));
            
            var result = await _reportsRep.GetAverageTicket(date);
            return Ok(result);
        }
    }
}