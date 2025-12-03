namespace unipos_basic_backend.src.DTOs
{
    public sealed class CashRegisterCardsDTO
    {
        public decimal InitialBalance { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal TotalProfit { get; set; }
    }
}