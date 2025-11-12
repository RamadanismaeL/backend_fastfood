namespace unipos_basic_backend.src.DTOs
{
    public sealed class UsersCreateDTO
    {
        public string Username { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Roles { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public IFormFile? Image { get; set; }
    }
}