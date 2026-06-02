namespace NotesApi.Exceptions
{
    /// <summary>
    /// Thrown when a requested resource does not exist or is not visible to the caller.
    /// Maps to HTTP 404 Not Found.
    /// </summary>
    public class NotFoundException : AppException
    {
        public NotFoundException(string resourceName, Guid id)
            : base(404, "not_found", $"{resourceName} with id '{id}' was not found.") { }

        public NotFoundException(string message)
            : base(404, "not_found", message) { }
    }

    /// <summary>
    /// Thrown when a uniqueness constraint is violated (e.g. duplicate email or username).
    /// Maps to HTTP 409 Conflict.
    /// </summary>
    public class ConflictException : AppException
    {
        public ConflictException(string message)
            : base(409, "conflict", message) { }
    }

    /// <summary>
    /// Thrown when the caller does not have permission to perform the operation.
    /// Maps to HTTP 403 Forbidden.
    /// </summary>
    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message = "You do not have permission to perform this action.")
            : base(403, "forbidden", message) { }
    }

    /// <summary>
    /// Thrown when incoming data fails business-rule validation that FluentValidation
    /// cannot catch (e.g. cross-field or database-dependent rules).
    /// Maps to HTTP 400 Bad Request.
    /// </summary>
    public class ValidationException : AppException
    {
        public ValidationException(string message)
            : base(400, "validation_error", message) { }
    }

    /// <summary>
    /// Thrown when authentication credentials are invalid.
    /// Maps to HTTP 401 Unauthorized.
    /// </summary>
    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message = "Invalid credentials.")
            : base(401, "unauthorized", message) { }
    }
}