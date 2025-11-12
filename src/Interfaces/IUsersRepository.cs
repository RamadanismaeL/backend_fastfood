using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IUsersRepository
    {
        Task<IEnumerable<UsersListDTO>> GetAllAsync();
        Task<ResponseDTO> CreateAsync(UsersCreateDTO user);
        Task<ResponseDTO> DeleteAsync(Guid id);
    }
}