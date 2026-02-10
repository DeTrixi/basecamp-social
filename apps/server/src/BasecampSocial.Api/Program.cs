using System.Text;
using System.Threading.RateLimiting;
using Amazon.S3;
using BasecampSocial.Api.Configuration;
using BasecampSocial.Api.Data;
using BasecampSocial.Api.Data.Entities;
using BasecampSocial.Api.Endpoints;
using BasecampSocial.Api.Hubs;
using BasecampSocial.Api.Middleware;
using BasecampSocial.Api.Services;
using BasecampSocial.Api.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
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

    // ── Database ─────────────────────────────────────────────
    // Register the AppDbContext with Npgsql (PostgreSQL provider).
    // The connection string comes from appsettings.json → ConnectionStrings:DefaultConnection.
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ── ASP.NET Core Identity ────────────────────────────
    builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // ── JWT Authentication ───────────────────────────────
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
    builder.Services.Configure<S3Options>(builder.Configuration.GetSection(S3Options.SectionName));
    builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Allow SignalR to receive the JWT via query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // ── FluentValidation ─────────────────────────────────
    builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

    // ── Application Services ─────────────────────────────
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IConversationService, ConversationService>();
    builder.Services.AddScoped<IMessageService, MessageService>();
    builder.Services.AddScoped<IKeyService, KeyService>();
    builder.Services.AddScoped<IPollService, PollService>();
    builder.Services.AddScoped<IUploadService, UploadService>();

    // ── S3 / MinIO Client ────────────────────────────────
    var s3Config = builder.Configuration.GetSection(S3Options.SectionName).Get<S3Options>()!;
    builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
        s3Config.AccessKey,
        s3Config.SecretKey,
        new AmazonS3Config
        {
            ServiceURL = s3Config.ServiceUrl,
            ForcePathStyle = s3Config.UsePathStyle
        }));

    // ── SignalR + Redis Backplane ─────────────────────────
    var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    builder.Services.AddSignalR()
        .AddStackExchangeRedis(redisConnection, options =>
        {
            options.Configuration.ChannelPrefix = RedisChannel.Literal("BasecampSocial");
        });

    // ── CORS ─────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>();
            var origins = corsOptions?.AllowedOrigins ?? ["http://localhost:8081"];
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

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
    app.UseSerilogRequestLogging();

    // ── Apply pending migrations ──────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    // ── Global error handling ────────────────────────────────
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // ── Rate limiting ────────────────────────────────────────
    app.UseRateLimiter();

    // ── CORS ─────────────────────────────────────────────────
    app.UseCors();

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

    // ── Auth middleware ──────────────────────────────────────
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Map API Endpoints ────────────────────────────────────
    app.MapAuthEndpoints();
    app.MapUserEndpoints();
    app.MapKeyEndpoints();
    app.MapConversationEndpoints();
    app.MapMessageEndpoints();
    app.MapPollEndpoints();
    app.MapUploadEndpoints();

    // ── Map SignalR Hub ──────────────────────────────────────
    app.MapHub<ChatHub>("/hubs/chat");

    await app.RunAsync();
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
