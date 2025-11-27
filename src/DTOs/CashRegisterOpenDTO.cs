namespace unipos_basic_backend.src.DTOs
{
    public sealed class CashRegisterOpenDTO
    {
        public decimal? OpeningBalance { get; set; }
        public Guid UserId { get; set; }
    }
}