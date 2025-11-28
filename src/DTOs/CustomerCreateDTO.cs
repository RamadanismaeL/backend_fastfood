namespace unipos_basic_backend.src.DTOs
{
    public sealed class CustomerCreateDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int OrderQty { get; set; }
    }
}