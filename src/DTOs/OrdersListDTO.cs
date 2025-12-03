namespace unipos_basic_backend.src.DTOs
{
    public sealed class OrdersListDTO
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TotalQty { get; set; }
        public decimal TotalPay { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalChange { get; set; }
        public OrderStatusEnum Status { get; set; }
        public string Operator { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public enum OrderStatusEnum
    {
        Cancelled,
        Pending,
        Paid
    }
}