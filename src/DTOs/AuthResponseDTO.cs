namespace unipos_basic_backend.src.DTOs
{
    public sealed class AuthResponseDTO
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}