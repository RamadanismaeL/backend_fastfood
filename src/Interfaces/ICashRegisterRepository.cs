using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface ICashRegisterRepository
    {
        Task<IEnumerable<CashRegisterListDTO>> GetAllAsync();
        Task<ResponseDTO> OpenRegisterAsync(CashRegisterOpenDTO cashRegister);
        Task<ResponseDTO> CloseRegisterAsync(CashRegisterCloseDTO cashRegister);
        Task<ResponseDTO> DeleteAsync(Guid id);
    }
}