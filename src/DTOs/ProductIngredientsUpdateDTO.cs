namespace unipos_basic_backend.src.DTOs
{
    public sealed class ProductIngredientsUpdateDTO
    {
        public Guid Id { get; set; }
        public Guid IngredientId { get; set; }
        public decimal? PackageSize { get; set; }
        public string? UnitOfMeasure { get; set; }
        public int Quantity { get; set; } = 0;
    }
}