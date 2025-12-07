namespace unipos_basic_backend.src.DTOs
{
    public sealed class ChartAreaReportDTO
    {
        public decimal[] Amounts { get; set; } = [];
        public string[] Date { get; set; } = [];
    }
}