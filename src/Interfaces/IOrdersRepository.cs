using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IOrdersRepository
    {
        Task<IEnumerable<OrdersListDTO>> GetAllAsync();
        Task<ResponseDTO> CreateAsync(OrdersCreateDTO order);
        Task<ResponseDTO> CreatePayNow(OrdersCreatePayNowDTO orderPayNow);
    }
}