namespace unipos_basic_backend.src.DTOs
{
    public sealed class OrdersCreateDTO
    {
        public string? CustomerFullName { get; set; }
        public string CustomerPhoneNumber { get; set; } = string.Empty;
        public List<OrderItemsDTO>? OrderItems { get; set; }
    }

    public sealed class OrderItemsDTO
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}