namespace BasecampSocial.Api.Configuration;

/// <summary>
/// Strongly-typed options for JWT authentication, bound from the "Jwt" section
/// of appsettings.json.
///
/// Using the Options pattern (IOptions&lt;JwtOptions&gt;) instead of reading
/// IConfiguration directly because:
/// 1. Compile-time safety — typos in key names are caught immediately.
/// 2. Dependency injection friendly — services receive only the config they need.
/// 3. Supports validation (via DataAnnotations or IValidateOptions).
/// 4. Supports hot-reload with IOptionsMonitor if needed later.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>The "iss" claim — identifies who issued the token.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>The "aud" claim — identifies the intended recipient (our mobile app).</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 secret key for signing tokens. Must be at least 64 characters
    /// in production. In a real deployment, this should come from a secrets manager
    /// (e.g. Azure Key Vault, AWS Secrets Manager) rather than appsettings.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>How long access tokens are valid. Short (15 min) to limit damage if stolen.</summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>How long refresh tokens are valid. Longer (30 days) for UX convenience.</summary>
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
