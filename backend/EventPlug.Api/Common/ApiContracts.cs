namespace EventPlug.Api.Common;

public sealed record ApiError(
    string Code,
    string Message,
    IDictionary<string, string[]>? FieldErrors = null);

public sealed record ListResponse<T>(IReadOnlyList<T> Items, int Total);

public static class ListResponse
{
    public static ListResponse<T> From<T>(IReadOnlyList<T> items) => new(items, items.Count);
}
