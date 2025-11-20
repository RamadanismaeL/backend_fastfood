namespace unipos_basic_backend.src.DTOs
{
    public sealed class IngredientsUpdateDTO
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public int Quantity { get; set; } = 0;
        public decimal UnitCostPrice { get; set; } = 0.00m;
        public DateTime ExpirationAt { get; set; }
        public bool IsActive { get; set; }
    }
}