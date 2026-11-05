namespace EventPlug.Api.Common;

/// <summary>Collects field errors and throws a single 400 with all of them.</summary>
public sealed class RequestValidator
{
    private readonly Dictionary<string, List<string>> errors = new();

    public IReadOnlyDictionary<string, List<string>> Errors => errors;

    public RequestValidator Check(bool isValid, string field, string message)
    {
        if (!isValid)
        {
            if (!errors.TryGetValue(field, out var list))
            {
                errors[field] = list = new List<string>();
            }

            list.Add(message);
        }

        return this;
    }

    public RequestValidator Required(string? value, string field, int maxLength)
    {
        Check(!string.IsNullOrWhiteSpace(value), field, "This field is required.");
        return MaxLength(value, field, maxLength);
    }

    /// <summary>For PATCH: null means "unchanged", but a provided value must not be blank.</summary>
    public RequestValidator NotBlankIfProvided(string? value, string field, int maxLength)
    {
        Check(value is null || !string.IsNullOrWhiteSpace(value), field, "This field cannot be blank.");
        return MaxLength(value, field, maxLength);
    }

    public RequestValidator MaxLength(string? value, string field, int maxLength) =>
        Check(value is null || value.Length <= maxLength, field, $"Must be at most {maxLength} characters.");

    public void ThrowIfInvalid()
    {
        if (errors.Count > 0)
        {
            throw ApiException.Validation(errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }
    }
}
