namespace unipos_basic_backend.src.DTOs
{
    public sealed class OrdersListDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TotalQty { get; set; }
        public decimal TotalPay { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}