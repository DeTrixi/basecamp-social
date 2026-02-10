namespace BasecampSocial.Api.Configuration;

/// <summary>
/// Strongly-typed CORS options, bound from the "Cors" section of appsettings.json.
///
/// CORS is configured to allow the React Native dev server (localhost:8081) during
/// development. In production, this should be locked down to only the domains that
/// need access (or removed entirely if the API is only accessed by mobile apps,
/// since native HTTP clients don't enforce CORS).
/// </summary>
public class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>List of allowed origins for cross-origin requests.</summary>
    public string[] AllowedOrigins { get; set; } = [];
}
