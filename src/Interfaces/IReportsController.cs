using Microsoft.AspNetCore.Mvc;
using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IReportsController
    {
        Task<ActionResult<IEnumerable<ReportsCashMovementsDTO>>> GetCashMovementsAsync([FromBody] DateDTO date);
        Task<ActionResult<IEnumerable<OrdersListDTO>>> GetOrdersDetailAsync([FromBody] DateDTO date);
        Task<ActionResult<IEnumerable<ReportsCardDTO>>> GetInitialBalance([FromBody] DateDTO date);
        Task<ActionResult<IEnumerable<ReportsCardDTO>>> GetInFlows([FromBody] DateDTO date);
        Task<ActionResult<IEnumerable<ReportsCardDTO>>> GetOutFlows([FromBody] DateDTO date);
        Task<ActionResult<IEnumerable<ReportsCardDTO>>> GetClosingBalance([FromBody] DateDTO date);
        Task<ActionResult<IEnumerable<ReportsCardNumDTO>>> GetNumOfSales([FromBody] DateDTO date);
        Task<ActionResult<IEnumerable<ReportsCardDTO>>> GetExpectedBalance([FromBody] DateDTO date);
        Task<ActionResult<IEnumerable<ReportsCardDTO>>> GetAverageTicket([FromBody] DateDTO date);
        Task<ActionResult<IEnumerable<CarouselPymtMethodDTO>>> GetPymtMethod([FromBody] DateDTO date);
        Task<ActionResult<IEnumerable<ChartAreaReportDTO>>> GetChartSalesPerHour([FromBody] DateDTO date);
        Task<ActionResult<IEnumerable<ReportsCardRecentSaleDTO>>> GetRecentSale([FromBody] DateDTO date);
    }
}