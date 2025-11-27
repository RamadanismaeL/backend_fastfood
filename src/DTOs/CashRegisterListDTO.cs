namespace unipos_basic_backend.src.DTOs
{
    public class CashRegisterListDTO
    {
        public Guid Id { get; set; }
        public bool Status { get; set; }
        public DateTime OpenedAt { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string Operator { get; set; } = string.Empty;
    }
}