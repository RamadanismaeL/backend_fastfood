namespace unipos_basic_backend.src.DTOs
{
    public sealed class OrdersCheckPosDTO
    {
        public Guid CashRegisterId { get; set; }
        public bool Status { get; set; }
    }
}