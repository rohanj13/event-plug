using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EventPlug.Api.Common;

public static class SwaggerSetup
{
    public const string BearerScheme = "Bearer";
    public const string OrgHeaderScheme = "OrgId";

    public static IServiceCollection AddEventPlugSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "EventPlug API",
                Version = "v1",
                Description =
                    "All /api/v1 endpoints require an Auth0 access token (Authorization: Bearer <token>) " +
                    "and the X-Org-Id header. IDs are UUIDs, enums are strings, money is a decimal string " +
                    "(e.g. \"1250.00\"). Lists return { items, total }; errors return { code, message, fieldErrors? }."
            });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Auth0 access token issued for the API audience."
            });

            options.AddSecurityDefinition(OrgHeaderScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-Org-Id",
                Description = "UUID of the organisation the request is scoped to."
            });

            options.SupportNonNullableReferenceTypes();
            options.NonNullableReferenceTypesAsRequired();
            options.UseAllOfToExtendReferenceSchemas();

            options.MapType<decimal>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "decimal",
                Example = new OpenApiString("0.00")
            });
            options.MapType<decimal?>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "decimal",
                Nullable = true
            });

            options.CustomSchemaIds(SchemaId);

            options.OperationFilter<SecureOperationFilter>();
        });

        return services;
    }

    // ListResponse<EventResponse> -> "EventResponseList"; nested types keep their parent prefix.
    private static string SchemaId(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ListResponse<>))
        {
            return SchemaId(type.GetGenericArguments()[0]) + "List";
        }

        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            return string.Concat(type.GetGenericArguments().Select(SchemaId)) + name;
        }

        return type.DeclaringType is null ? type.Name : SchemaId(type.DeclaringType) + type.Name;
    }

    private sealed class SecureOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var path = "/" + (context.ApiDescription.RelativePath ?? string.Empty);
            if (!path.StartsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = BearerScheme } }] = Array.Empty<string>(),
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = OrgHeaderScheme } }] = Array.Empty<string>()
            });
        }
    }
}
