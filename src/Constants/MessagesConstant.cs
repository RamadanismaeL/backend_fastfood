namespace unipos_basic_backend.src.Constants
{
    public static class MessagesConstant
    {
        public const string Saved       = "Record saved successfully.";
        public const string Created     = "Record created successfully.";
        public const string Updated     = "Record updated successfully.";
        public const string Deleted     = "Record deleted successfully.";
        
        public const string AlreadyExists = "This record already exists.";
        public const string NotFound       = "The requested record could not be found.";
        public const string NoChanges      = "No changes were detected.";     
        public const string InvalidData    = "Please correct the highlighted errors and try again.";

        public const string OperationFailed = "The operation could not be completed. Please try again.";

        public const string ServerError     = "An unexpected error occurred.";
        public const string Unauthorized    = "You are not authorised to perform this action.";
        public const string Forbidden       = "Access to this resource is forbidden.";        
    }
}