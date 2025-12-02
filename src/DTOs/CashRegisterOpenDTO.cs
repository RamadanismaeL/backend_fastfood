namespace unipos_basic_backend.src.DTOs
{
    public sealed class CashRegisterOpenDTO
    {
        public Guid UserId { get; set; }
        public decimal? Amount { get; set; }
    }
}