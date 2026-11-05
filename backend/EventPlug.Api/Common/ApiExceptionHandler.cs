using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventPlug.Api.Common;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        exception = Unwrap(exception);

        var (status, error) = exception switch
        {
            ApiException api => (api.StatusCode, new ApiError(api.Code, api.Message, api.FieldErrors)),
            BadHttpRequestException { InnerException: JsonException json } =>
                (StatusCodes.Status400BadRequest, new ApiError("invalid_body", $"The request body is invalid. {json.Message}")),
            JsonException json =>
                (StatusCodes.Status400BadRequest, new ApiError("invalid_body", $"The request body is invalid. {json.Message}")),
            BadHttpRequestException bad =>
                (StatusCodes.Status400BadRequest, new ApiError("bad_request", bad.Message)),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
                (StatusCodes.Status409Conflict, new ApiError("conflict", "This record is referenced by other records or references a record that does not exist.")),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, new ApiError("conflict", "A record with the same unique values already exists.")),
            _ => (StatusCodes.Status500InternalServerError, new ApiError("internal_error", "An unexpected error occurred."))
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception");
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }

    // EF Core wraps exceptions thrown while evaluating query parameters; surface our own ApiException.
    private static Exception Unwrap(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is ApiException)
            {
                return current;
            }
        }

        return exception;
    }
}
