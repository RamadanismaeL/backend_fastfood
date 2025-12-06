namespace unipos_basic_backend.src.DTOs
{
    public sealed class ReportsCardDTO
    {
        public decimal TotalAmount { get; set; }
        public int TrendPercentage { get; set; }
        public DateTime? LastUpdated { get; set; }
        public decimal[] ChartAmount { get; set; } = [];
        public string[] ChartDate { get; set; } = [];
    }
}