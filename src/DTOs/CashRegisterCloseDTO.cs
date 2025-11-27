namespace unipos_basic_backend.src.DTOs
{
    public sealed class CashRegisterCloseDTO
    {
        public Guid Id { get; set; }
        public decimal? ClosingBalance { get; set; }
    }
}