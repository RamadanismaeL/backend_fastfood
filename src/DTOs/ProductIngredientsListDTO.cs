namespace unipos_basic_backend.src.DTOs
{
    public sealed class ProductIngredientsListDTO
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 0;
    }
}