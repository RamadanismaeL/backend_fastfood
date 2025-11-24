using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IIngredientsRepository
    {
        Task<IEnumerable<IngredientsListDTO>> GetAllAsync();
        Task<ResponseDTO> CreateAsync(IngredientsCreateDTO ingredient);
        Task<ResponseDTO> UpdateAsync(IngredientsUpdateDTO ingredient);
        Task<ResponseDTO> DeleteAsync(Guid id);
        Task<IngredientsCardsDTO> GetCardAsync();
    }
}