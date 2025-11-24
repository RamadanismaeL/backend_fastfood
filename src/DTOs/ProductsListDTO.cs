namespace unipos_basic_backend.src.DTOs
{
    public sealed class ProductsListDTO
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal? Price { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}