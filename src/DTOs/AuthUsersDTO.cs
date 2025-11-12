namespace unipos_basic_backend.src.DTOs
{
    public sealed class AuthUsersDTO
    {
        public Guid Id { get; set; }        
        public string Username { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool Is_Active { get; set; }
    }
}