namespace unipos_basic_backend.src.DTOs
{
    public sealed class OrdersCreatePayNowDTO
    {
        public decimal TotalPaid { get; set; }
        public decimal TotalChange { get; set; }
        public PymtMethodDTO? Method { get; set; }
        public List<OrderItemsDTO>? OrderItems { get; set; }
    }

    public sealed class PymtMethodDTO
    {
        public decimal? Cash { get; set; }
        public decimal? EMola { get; set; }
        public decimal? MPesa { get; set; }
    }
}