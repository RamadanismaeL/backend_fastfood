namespace unipos_basic_backend.src.DTOs
{
    public sealed class CashRegisterListDTO
    {
        public Guid Id { get; set; }
        public string Operator { get; set; } = string.Empty;
        public bool Status { get; set; }
        public decimal? TotalOpened { get; set; }
        public decimal? TotalClosed { get; set; }
        public DateTime OpenedAt { get; set; }                
        public DateTime? ClosedAt { get; set; }     
    }
}