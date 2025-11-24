namespace unipos_basic_backend.src.DTOs
{
    public sealed class IngredientsCardsDTO
    {
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int TotalActiveQty { get; set; }
        public int NearExpiryCount { get; set; }
        public int ExpiredCount { get; set; }
    }
}