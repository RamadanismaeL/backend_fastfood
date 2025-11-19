namespace unipos_basic_backend.src.DTOs
{
    public sealed class UsersListDTO
    {    
        public string Username { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Roles { get; set; } = string.Empty;
        public string? Image { get; set; }
        public bool Is_Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}