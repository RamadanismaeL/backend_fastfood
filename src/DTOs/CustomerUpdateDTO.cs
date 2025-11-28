namespace unipos_basic_backend.src.DTOs
{
    public sealed class CustomerUpdateDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int OrderQty { get; set; }
    }
}