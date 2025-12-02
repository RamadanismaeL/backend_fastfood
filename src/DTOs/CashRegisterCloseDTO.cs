namespace unipos_basic_backend.src.DTOs
{
    public sealed class CashRegisterCloseDTO
    {
        public Guid CashRegisterId { get; set; }
        public decimal? Amount { get; set; }
    }
}