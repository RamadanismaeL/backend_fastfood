namespace unipos_basic_backend.src.DTOs
{
    public sealed class OrdersUpdatePayNowDTO
    {
        public Guid SaleId { get; set; }
        public PymtMethodDTO? Method { get; set; }
    }
}