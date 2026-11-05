namespace EventPlug.Api.Common;

public class ApiException : Exception
{
    public ApiException(
        int statusCode,
        string code,
        string message,
        IDictionary<string, string[]>? fieldErrors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        FieldErrors = fieldErrors;
    }

    public int StatusCode { get; }
    public string Code { get; }
    public IDictionary<string, string[]>? FieldErrors { get; }

    public static ApiException Unauthorized() =>
        new(StatusCodes.Status401Unauthorized, "unauthorized", "A signed-in organisation member is required.");

    public static ApiException NotFound(string resource) =>
        new(StatusCodes.Status404NotFound, "not_found", $"{resource} was not found.");

    public static ApiException Conflict(string message) =>
        new(StatusCodes.Status409Conflict, "conflict", message);

    public static ApiException Forbidden(string message = "You do not have permission to perform this action.") =>
        new(StatusCodes.Status403Forbidden, "forbidden", message);

    public static ApiException BadRequest(string message) =>
        new(StatusCodes.Status400BadRequest, "bad_request", message);

    public static ApiException Validation(IDictionary<string, string[]> fieldErrors) =>
        new(StatusCodes.Status400BadRequest, "validation_failed", "One or more fields are invalid.", fieldErrors);
}
