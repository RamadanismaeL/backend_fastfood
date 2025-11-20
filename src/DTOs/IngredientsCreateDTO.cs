namespace unipos_basic_backend.src.DTOs
{
    public sealed class IngredientsCreateDTO
    {
        public string ItemName { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public int Quantity { get; set; } = 0;
        public decimal UnitCostPrice { get; set; } = 0.00m;
        public DateTime ExpirationAt { get; set; }
    }
}