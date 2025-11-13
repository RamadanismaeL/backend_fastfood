namespace unipos_basic_backend.src.DTOs
{
    public sealed class AuthCheckSessionDTO
    {
        public bool ServerOk { get; set; }
        public bool Is_LoggedIn { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}