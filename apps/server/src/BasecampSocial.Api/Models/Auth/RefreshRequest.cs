namespace BasecampSocial.Api.Models.Auth;

/// <summary>Request body for POST /api/v1/auth/refresh.</summary>
public sealed record RefreshRequest(string RefreshToken);
