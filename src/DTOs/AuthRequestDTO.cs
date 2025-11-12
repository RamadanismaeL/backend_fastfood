namespace unipos_basic_backend.src.DTOs
{
    public sealed class AuthRequestDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}