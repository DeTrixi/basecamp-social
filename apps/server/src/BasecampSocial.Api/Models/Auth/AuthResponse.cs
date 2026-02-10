namespace BasecampSocial.Api.Models.Auth;

/// <summary>Response body from all auth endpoints (login, register, refresh).</summary>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserInfo User);

/// <summary>Minimal user info embedded in auth responses.</summary>
public sealed record UserInfo(
    Guid Id,
    string UserName,
    string DisplayName,
    string? AvatarUrl);
