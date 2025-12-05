using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IReportsRepository
    {
        Task<IEnumerable<ReportsCashMovementsDTO>> GetCashMovementsAsync(DateDTO date);

        Task<IEnumerable<OrdersListDTO>> GetOrdersDetailAsync(DateDTO date);
    }
}