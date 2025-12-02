namespace unipos_basic_backend.src.DTOs
{
    public sealed class OrdersUpdatePayNowDTO
    {
        public Guid Id { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalChange { get; set; }
        public PymtMethodDTO? Method { get; set; }
    }
}