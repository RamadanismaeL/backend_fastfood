using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IOrdersRepository
    {
        Task<OrdersCheckPosDTO?> CheckPos(Guid userId);
        Task<IEnumerable<OrdersListDTO>> GetAllAsync(Guid registerId);
        Task<IEnumerable<OrdersListDTO>> GetAllAsync();
        Task<ResponseDTO> CreateAsync(OrdersCreateDTO order);
        Task<ResponseDTO> CreatePayNow(OrdersCreatePayNowDTO order);
        Task<string> GetReceiptNumber();
        Task<ResponseDTO> UpdatePayNow(OrdersUpdatePayNowDTO order);
    }
}