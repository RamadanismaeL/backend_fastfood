namespace unipos_basic_backend.src.DTOs
{
    public sealed class ReportsCardNumDTO
    {
        public long TotalAmount { get; set; }
        public long TrendPercentage { get; set; }
        public DateTime? LastUpdated { get; set; }
        public long[] ChartAmount { get; set; } = [];
        public string[] ChartDate { get; set; } = [];
    }
}