using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace UtilitiesPortal.Api.Errors;

/// <summary>
/// The one place where exceptions become HTTP responses. The client always gets a small JSON
/// error (ProblemDetails) and never a stack trace or raw SQL error text.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = Map(exception);

        if (status >= 500)
        {
            // Only real server faults are logged with their stack trace.
            _logger.LogError(exception, "Unhandled exception");
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title },
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true; // handled: no other handler needs to run
    }

    private static (int Status, string Title) Map(Exception exception)
    {
        // Problems our own code raised on purpose
        if (exception is AppException app)
        {
            return (app.StatusCode, app.Message);
        }

        // Problems the database reported while saving
        if (exception is DbUpdateException { InnerException: SqlException sql })
        {
            // 50001 = our trigger: a meter reading went backwards
            if (sql.Errors.Cast<SqlError>().Any(e => e.Number == 50001))
            {
                return (StatusCodes.Status409Conflict,
                    "Meter reading cannot be lower than an earlier reading for the same meter.");
            }

            // 2601 / 2627 = unique constraint or unique index violated
            if (sql.Number is 2601 or 2627)
            {
                return (StatusCodes.Status409Conflict, "That record already exists.");
            }

            // 547 = a CHECK or FOREIGN KEY constraint was violated
            if (sql.Number == 547)
            {
                return (StatusCodes.Status400BadRequest,
                    "The data breaks a database rule. Check the values and try again.");
            }
        }

        return (StatusCodes.Status500InternalServerError, "Something went wrong on the server.");
    }
}
