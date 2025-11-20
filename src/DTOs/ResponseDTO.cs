namespace unipos_basic_backend.src.DTOs
{
    public sealed class ResponseDTO
    {
        public bool IsSuccess { get; set; } = false;
        public string Message { get; set; } = string.Empty;

        public static ResponseDTO Success(string message) => new() { IsSuccess = true, Message = message };

        public static ResponseDTO Failure(string message) => new() { IsSuccess = false, Message = message };
    }
}