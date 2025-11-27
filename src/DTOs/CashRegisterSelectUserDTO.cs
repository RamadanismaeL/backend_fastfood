namespace unipos_basic_backend.src.DTOs
{
    public sealed class CashRegisterSelectUserDTO
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}