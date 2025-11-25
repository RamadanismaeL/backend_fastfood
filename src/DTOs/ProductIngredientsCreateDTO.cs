namespace unipos_basic_backend.src.DTOs
{
    public sealed class ProductIngredientsCreateDTO
    {
        public Guid ProductId { get; set; }
        public Guid IngredientId { get; set; }
        public int Quantity { get; set; } = 0;
    }
}