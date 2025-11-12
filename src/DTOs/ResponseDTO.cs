namespace unipos_basic_backend.src.DTOs
{
    public sealed class ResponseDTO
    {
        public bool IsSuccess { get; set; } = false;
        public string Message { get; set; } = string.Empty;
    }
}