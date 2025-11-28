using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface ICustomerRepository
    {
        Task<IEnumerable<CustomerListDTO>> GetAllAsync();
        Task<ResponseDTO> CreateAsync(CustomerCreateDTO customer);
        Task<ResponseDTO> UpdateAsync(CustomerUpdateDTO customer);
        Task<ResponseDTO> DeleteAsync(Guid id);
    }
}