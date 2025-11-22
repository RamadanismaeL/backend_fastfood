namespace unipos_basic_backend.src.DTOs
{
    public sealed class IngredientsCreateDTO
    {
        public string ItemName { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public int? PackageSize { get; set; }
        public string? UnitOfMeasure { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitCostPrice { get; set; }
        public DateTime? ExpirationAt { get; set; }
    }
}