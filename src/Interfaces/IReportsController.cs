using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IReportsController
    {
        Task<ActionResult<IEnumerable<ReportsCashMovementsDTO>>> GetCashMovementsAsync([FromBody] DateDTO date);

        Task<ActionResult<IEnumerable<OrdersListDTO>>> GetOrdersDetailAsync([FromBody] DateDTO date);
    }
}