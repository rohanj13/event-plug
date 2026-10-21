using EventPlug.Api.Data;
using EventPlug.Api.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ITenantContext, TenantContext>();

var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];
var auth0IsConfigured =
    !string.IsNullOrWhiteSpace(auth0Domain) &&
    !string.IsNullOrWhiteSpace(auth0Audience);

builder.Services.AddAuthorization();

if (auth0IsConfigured)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{auth0Domain!.TrimEnd('/')}/";
            options.Audience = auth0Audience;
        });
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddDbContext<EventPlugDbContext>(options =>
{
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
        .UseSnakeCaseNamingConvention();
});

var app = builder.Build();

app.UseCors();

if (auth0IsConfigured)
{
    app.UseAuthentication();
    app.UseMiddleware<TenantResolutionMiddleware>();
    app.UseAuthorization();
}

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "event-plug-api",
    timestamp = DateTimeOffset.UtcNow
}));

api.MapGet("/db-check", async (IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("Postgres")
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

app.Run();
