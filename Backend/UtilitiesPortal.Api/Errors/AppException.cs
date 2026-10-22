namespace UtilitiesPortal.Api.Errors;

/// <summary>
/// An expected problem with a request (not found, conflict, bad data). Services throw these and
/// GlobalExceptionHandler turns them into a clean JSON error, so controllers stay short.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>404: the thing asked for doesn't exist (or the caller isn't allowed to know it does).</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(StatusCodes.Status404NotFound, message)
    {
    }
}

/// <summary>409: the request is valid but clashes with existing data (for example, a duplicate).</summary>
public class ConflictException : AppException
{
    public ConflictException(string message) : base(StatusCodes.Status409Conflict, message)
    {
    }
}

/// <summary>400: the request breaks a business rule.</summary>
public class BadRequestException : AppException
{
    public BadRequestException(string message) : base(StatusCodes.Status400BadRequest, message)
    {
    }
}
