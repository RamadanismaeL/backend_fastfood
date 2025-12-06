namespace unipos_basic_backend.src.DTOs
{
    public sealed class ReportsCardResponseDTO
    {
        public DateTime? LastUpdated { get; set; }
        public decimal[] TotalAmount { get; set; } = [];
        public string[] Date { get; set; } = [];
    }
}