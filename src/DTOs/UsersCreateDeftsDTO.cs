namespace unipos_basic_backend.src.DTOs
{
    public sealed class UsersCreateDeftsDTO
    {
        public string Username { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }
}