namespace EventPlug.Api.Common;

public static class EndpointMetadataExtensions
{
    /// <summary>Documents error responses that use the shared { code, message, fieldErrors? } shape.</summary>
    public static RouteHandlerBuilder ProducesApiErrors(this RouteHandlerBuilder builder, params int[] statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            builder.Produces<ApiError>(statusCode);
        }

        return builder;
    }
}
