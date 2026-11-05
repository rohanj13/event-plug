using System.Text.Json.Serialization;
using EventPlug.Api.Common;
using EventPlug.Api.Data;
using EventPlug.Api.Endpoints;
using EventPlug.Api.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<CurrentUser>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new DecimalStringConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new DecimalStringConverter());
});

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEventPlugSwagger();

var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];
var auth0IsConfigured =
    !string.IsNullOrWhiteSpace(auth0Domain) &&
    !string.IsNullOrWhiteSpace(auth0Audience);

var devAuthSection = builder.Configuration.GetSection(DevAuthOptions.Section);
var devAuth = devAuthSection.Get<DevAuthOptions>() ?? new DevAuthOptions();
builder.Services.Configure<DevAuthOptions>(devAuthSection);

if (devAuth.Enabled)
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("DevAuth can only be enabled in the Development environment.");
    }

    if (string.IsNullOrWhiteSpace(devAuth.Subject))
    {
        throw new InvalidOperationException("DevAuth:Subject must be set to the auth0_subject of an existing user.");
    }
}

var demoAuthSection = builder.Configuration.GetSection(DemoAuthOptions.Section);
var demoAuth = demoAuthSection.Get<DemoAuthOptions>() ?? new DemoAuthOptions();
builder.Services.Configure<DemoAuthOptions>(demoAuthSection);
demoAuth.Validate();

var authIsEnabled = auth0IsConfigured || devAuth.Enabled || demoAuth.Enabled;

builder.Services.AddAuthorization();

if (devAuth.Enabled)
{
    builder.Services
        .AddAuthentication(DevAuthOptions.Scheme)
        .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>(DevAuthOptions.Scheme, _ => { });
}
else if (demoAuth.Enabled)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = demoAuth.CreateValidationParameters();
        });
}
else if (auth0IsConfigured)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{auth0Domain!.TrimEnd('/')}/";
            options.Audience = auth0Audience;
        });
}

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var postgresConnectionString = PostgresConnectionString.Normalize(builder.Configuration.GetConnectionString("Postgres"));

builder.Services.AddDbContext<EventPlugDbContext>(options =>
{
    options
        .UseNpgsql(postgresConnectionString)
        .UseSnakeCaseNamingConvention();
});

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<EventPlugDbContext>().Database.Migrate();
}

if (demoAuth.Enabled && demoAuth.SeedOnStartup)
{
    using var scope = app.Services.CreateScope();
    await DemoDataSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<EventPlugDbContext>(), demoAuth.OrgId);
}

app.UseExceptionHandler();
app.UseCors();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (devAuth.Enabled)
{
    app.Logger.LogWarning(
        "DevAuth is ON: Auth0 is bypassed and every request runs as {Subject}. Development only.",
        devAuth.Subject);
}
else if (demoAuth.Enabled)
{
    app.Logger.LogWarning(
        "DemoAuth is ON: anyone can sign in as a demo persona in org {OrgId}. Use only for sample data.",
        demoAuth.OrgId);
}

if (authIsEnabled)
{
    app.UseAuthentication();
    app.UseMiddleware<TenantResolutionMiddleware>();
    app.UseAuthorization();
}

var api = app.MapGroup("/api").ExcludeFromDescription();

api.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "event-plug-api",
    timestamp = DateTimeOffset.UtcNow
}));

api.MapGet("/db-check", async (IConfiguration configuration) =>
{
    var connectionString = PostgresConnectionString.Normalize(configuration.GetConnectionString("Postgres"))
        ?? "Host=postgres;Port=5432;Database=eventplug;Username=eventplug";

    try
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        var result = await command.ExecuteScalarAsync();
        return Results.Ok(new { connected = Convert.ToInt32(result) == 1 });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database connection failed: {ex.Message}", statusCode: 503);
    }
});

var v1 = app.MapGroup("/api/v1").WithOpenApi();
foreach (var status in new[] { StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden, StatusCodes.Status500InternalServerError })
{
    v1.WithMetadata(new ProducesResponseTypeMetadata(status, typeof(ApiError), new[] { "application/json" }));
}

if (authIsEnabled)
{
    v1.RequireAuthorization();
}

v1.MapDemoAuthEndpoints();
v1.MapIdentityEndpoints();
v1.MapTeamEndpoints();
v1.MapEventEndpoints();
v1.MapGuestEndpoints();
v1.MapRunSheetEndpoints();
v1.MapBudgetEndpoints();
v1.MapVendorEndpoints();

app.Run();

public partial class Program;
