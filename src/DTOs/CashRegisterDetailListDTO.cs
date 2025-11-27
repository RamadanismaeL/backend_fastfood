namespace unipos_basic_backend.src.DTOs
{
    public sealed class CashRegisterDetailListDTO
    {
        public Guid Id { get; set; }
        public string CashName { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Description { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }        
    }
}