using System.Threading.RateLimiting;
using BasecampSocial.Api.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

// ──────────────────────────────────────────────────────────────
// Bootstrap logger — captures any errors during startup before
// the host is fully built (e.g. bad config, missing connection).
// Once the host is built, this is replaced by the full logger
// configured from appsettings.json.
// ──────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Basecamp Social API...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ──────────────────────────────────────────────
    // Replaces the default ASP.NET Core logging with Serilog.
    // Configuration is read from the "Serilog" section in
    // appsettings.json (sinks, enrichers, minimum levels).
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
                     .ReadFrom.Services(services));

    // ── Swagger / OpenAPI ─────────────────────────────────
    // AddOpenApi generates the OpenAPI spec at /openapi/v1.json.
    // AddSwaggerGen adds Swashbuckle's Swagger UI for interactive
    // API testing at /swagger. The JWT bearer definition lets you
    // paste an access token into the UI to test authenticated endpoints.
    builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
        {
            Title = "Basecamp Social API",
            Version = "v1",
            Description = "E2E encrypted chat API — the server is zero-knowledge.",
            License = new Microsoft.OpenApi.OpenApiLicense
            {
                Name = "AGPL-3.0",
                Url = new Uri("https://www.gnu.org/licenses/agpl-3.0.html")
            }
        });

        // JWT Bearer auth button in Swagger UI
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Description = "Enter your JWT token: Bearer {token}",
            Name = "Authorization",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        options.AddSecurityRequirement(_ => new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer"),
                new List<string>()
            }
        });
    });

    // ── Rate Limiting ────────────────────────────────────────
    // Protects against brute-force attacks and abuse. Uses a
    // fixed-window strategy per IP address:
    // - "fixed": general limit (100 requests/minute) for all endpoints
    // - "auth":  strict limit (10 requests/minute) for auth endpoints
    //   to prevent credential stuffing attacks.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("fixed", limiter =>
        {
            limiter.PermitLimit = 100;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 0;
        });

        options.AddFixedWindowLimiter("auth", limiter =>
        {
            limiter.PermitLimit = 10;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 0;
        });

        // Use client IP as the partition key
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                }));
    });

    var app = builder.Build();

    // ── Serilog request logging ──────────────────────────────
    // Logs a single structured event per HTTP request with
    // method, path, status code, and elapsed ms. Replaces the
    // noisy default ASP.NET Core request logging (which emits
    // multiple log events per request).
    app.UseSerilogRequestLogging();

    // ── Global error handling ────────────────────────────────
    // Must be early in the pipeline to catch exceptions from all
    // downstream middleware and endpoints.
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // ── Rate limiting ────────────────────────────────────────
    app.UseRateLimiter();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Basecamp Social API v1");
            options.DocumentTitle = "Basecamp Social API";
        });
    }

    app.UseHttpsRedirection();

    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    // Ensures all buffered log events are flushed to sinks
    // before the process exits (important for Seq).
    Log.CloseAndFlush();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
