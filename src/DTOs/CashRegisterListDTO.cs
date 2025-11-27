namespace unipos_basic_backend.src.DTOs
{
    public sealed class CashRegisterListDTO
    {
        public Guid Id { get; set; }
        public string Operator { get; set; } = string.Empty;
        public bool Status { get; set; }
        public decimal? OpeningBalance { get; set; }
        public DateTime OpenedAt { get; set; }        
        public decimal? TotalCashIn { get; set; }
        public decimal? TotalCashOut { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal? ClosingBalance { get; set; }        
    }
}