namespace unipos_basic_backend.src.DTOs
{
    public sealed class ProductsUpdateDTO
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public IFormFile? ImageUrl { get; set; }
        public decimal? Price { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
    }
}