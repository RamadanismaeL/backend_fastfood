namespace unipos_basic_backend.src.DTOs
{
    public sealed class ReportsCardResponseNumDTO
    {
        public DateTime? LastUpdated { get; set; }
        public long[] TotalAmount { get; set; } = [];
        public string[] Date { get; set; } = [];
    }
}