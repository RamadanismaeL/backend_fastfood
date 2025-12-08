namespace unipos_basic_backend.src.Constants
{
    public static class MessagesConstant
    {
        public const string Saved       = "SERVER_MESSAGES.RECORD_SAVED_SUCCESS";
        public const string Created     = "SERVER_MESSAGES.RECORD_CREATED_SUCCESS";
        public const string Updated     = "SERVER_MESSAGES.RECORD_UPDATED_SUCCESS";
        public const string Deleted     = "SERVER_MESSAGES.RECORD_DELETED_SUCCESS";
        
        public const string AlreadyExists = "SERVER_MESSAGES.RECORD_ALREADY_EXISTS";
        public const string NotFound       = "SERVER_MESSAGES.RECORD_NOT_FOUND";
        public const string NoChanges      = "SERVER_MESSAGES.NO_CHANGES_DETECTED";     
        public const string InvalidData    = "SERVER_MESSAGES.CORRECT_HIGHLIGHTED_ERRORS";

        public const string OperationFailed = "SERVER_MESSAGES.OPERATION_FAILED";

        public const string CashOpened = "SERVER_MESSAGES.CASH_REGISTER_OPENED";
        public const string CashOpenedError = "SERVER_MESSAGES.CASH_REGISTER_ALREADY_OPEN";
        public const string CashClosed = "SERVER_MESSAGES.CASH_REGISTER_CLOSED";
        public const string InsufIngredient = "SERVER_MESSAGES.INSUFFICIENT_INGREDIENT";

        public const string ServerError     = "SERVER_MESSAGES.UNEXPECTED_ERROR";
        public const string Unauthorized    = "SERVER_MESSAGES.UNAUTHORISED_ACTION";
        public const string Forbidden       = "SERVER_MESSAGES.ACCESS_FORBIDDEN";        
    }
}