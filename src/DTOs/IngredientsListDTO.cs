namespace unipos_basic_backend.src.DTOs
{
    public sealed class IngredientsListDTO
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public int? PackageSize { get; set; }
        public string? UnitOfMeasure { get; set; }
        public int Quantity { get; set; } = 0;
        public decimal UnitCostPrice { get; set; } = 0.00m;
        public decimal TotalCostPrice { get; set; } = 0.00m;
        public DateTime? ExpirationAt { get; set; }
        public string? ExpirationStatus { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}