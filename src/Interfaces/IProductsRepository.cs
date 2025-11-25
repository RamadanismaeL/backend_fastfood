using unipos_basic_backend.src.DTOs;

namespace unipos_basic_backend.src.Interfaces
{
    public interface IProductsRepository
    {
        Task<IEnumerable<ProductsListDTO>> GetAllAsync();
        Task<ResponseDTO> CreateAsync(ProductsCreateDTO products);
        Task<ResponseDTO> UpdateAsync(ProductsUpdateDTO products);
        Task<ResponseDTO> DeleteAsync(Guid id);
        
        Task<IEnumerable<ProductIngredientsListDTO>> GetProductIngredient(Guid productId);
        Task<ResponseDTO> CreateProductIngredient(ProductIngredientsCreateDTO productIngredient);
        Task<ResponseDTO> UpdateProductIngredient(ProductIngredientsUpdateDTO productIngredient);
        Task<ResponseDTO> DeleteProductIngredient(Guid id);
        Task<IEnumerable<ProductIngredientSelectIngredientDTO>> GetSelectIngredient();
    }
}