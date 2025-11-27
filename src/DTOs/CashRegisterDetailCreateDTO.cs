namespace unipos_basic_backend.src.DTOs
{
    public sealed class CashRegisterDetailCreateDTO
    {
        public Guid CashRegisterId { get; set; }
        public string CashName { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string Description { get; set; } = string.Empty; 
    }
}