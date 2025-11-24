namespace unipos_basic_backend.src.DTOs
{
    public sealed class ProductsCreateDTO
    {
        public string ItemName { get; set; } = string.Empty;
        public IFormFile? ImageUrl { get; set; }
        public decimal? Price { get; set; }
        public string? Category { get; set; }
    }
}