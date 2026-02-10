namespace BasecampSocial.Api.Data.Entities;

/// <summary>
/// Stores a JWT refresh token issued to a user during authentication.
/// 
/// Design decisions:
/// - The JWT auth strategy uses short-lived access tokens (15 min) paired with
///   long-lived refresh tokens (30 days). This limits the damage window if an
///   access token is stolen, while keeping the UX smooth (no constant re-login).
/// - Refresh tokens are stored server-side (not just in a cookie) so the server
///   can revoke them on logout, password change, or suspicious activity.
/// - <see cref="Token"/> is a cryptographically random string (not a JWT) — it's
///   opaque to the client and only meaningful when looked up in the database.
/// - <see cref="RevokedAt"/> is nullable: null means the token is still valid.
///   We track *when* it was revoked for audit purposes rather than just deleting it.
/// - The computed properties <see cref="IsRevoked"/>, <see cref="IsExpired"/>, and
///   <see cref="IsActive"/> are not mapped to database columns — they exist purely
///   for clean domain logic in the auth service.
/// - One user can have multiple active refresh tokens (one per device/session),
///   hence the one-to-many relationship with <see cref="AppUser"/>.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Cryptographically random opaque token string sent to the client.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>When this refresh token expires (typically 30 days after creation).</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Set when the token is explicitly revoked (logout, password change). Null = not revoked.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Computed: true if the token has been explicitly revoked.</summary>
    public bool IsRevoked => RevokedAt is not null;

    /// <summary>Computed: true if the token has passed its expiration date.</summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>Computed: true if the token is neither revoked nor expired.</summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation
    public AppUser User { get; set; } = null!;
}
