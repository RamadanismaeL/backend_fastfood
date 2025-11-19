namespace unipos_basic_backend.src.DTOs
{
    public sealed class UsersUpdateDTO
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Roles { get; set; } = string.Empty;
        public bool Is_Active { get; set; }
        public string? Password { get; set; }
    }
}