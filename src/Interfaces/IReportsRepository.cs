using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IReportsRepository
    {
        Task<IEnumerable<ReportsCashMovementsDTO>> GetCashMovementsAsync(DateDTO date);
        Task<IEnumerable<OrdersListDTO>> GetOrdersDetailAsync(DateDTO date);
        Task<IEnumerable<ReportsCardDTO>> GetInitialBalance(DateDTO date);
        Task<IEnumerable<ReportsCardDTO>> GetInFlows(DateDTO date);
        Task<IEnumerable<ReportsCardDTO>> GetOutFlows(DateDTO date);
        Task<IEnumerable<ReportsCardDTO>> GetClosingBalance(DateDTO date);
        Task<IEnumerable<ReportsCardNumDTO>> GetNumOfSales(DateDTO date);
        Task<IEnumerable<ReportsCardDTO>> GetExpectedBalance(DateDTO date);
        Task<IEnumerable<ReportsCardDTO>> GetAverageTicket(DateDTO date);
        Task<IEnumerable<CarouselPymtMethodDTO>> GetPymtMethod(DateDTO date);
        Task<IEnumerable<ChartAreaReportDTO>> GetChartSalesPerHour(DateDTO date);
        Task<IEnumerable<ReportsCardRecentSaleDTO>> GetRecentSale(DateDTO date);
    }
}