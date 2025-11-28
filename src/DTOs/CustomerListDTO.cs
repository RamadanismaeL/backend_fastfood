namespace unipos_basic_backend.src.DTOs
{
    public sealed class CustomerListDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int OrderQty { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}