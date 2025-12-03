namespace unipos_basic_backend.src.DTOs
{
    public sealed class OrdersCreateDTO
    {
        public Guid CashRegisterId { get; set; }
        public string? CustomerName { get; set; }
        public string CustomerPhone { get; set; } = string.Empty;
        public List<OrderItemsDTO>? OrderItems { get; set; }
    }

    public sealed class OrderItemsDTO
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}