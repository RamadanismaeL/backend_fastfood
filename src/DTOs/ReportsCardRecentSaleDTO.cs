namespace unipos_basic_backend.src.DTOs
{
    public sealed class ReportsCardRecentSaleDTO
    {
        public Guid Id { get; set; }
        public long OrderNumber { get; set; }
        public string[] Methods { get; set; } = [];
        public decimal TotalPay { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;

    }
}