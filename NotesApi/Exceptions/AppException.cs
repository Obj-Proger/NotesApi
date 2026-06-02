namespace NotesApi.Exceptions
{
    /// <summary>
    /// Base class for all domain-specific exceptions in the application.
    /// Carries an HTTP status code so the exception handler can map it
    /// to the correct HTTP response without any switch/catch chains.
    /// </summary>
    public abstract class AppException : Exception
    {
        /// <summary>HTTP status code that should be returned to the client.</summary>
        public int StatusCode { get; }

        /// <summary>Short machine-readable error code (e.g. "not_found", "conflict").</summary>
        public string ErrorCode { get; }

        protected AppException(int statusCode, string errorCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }
}