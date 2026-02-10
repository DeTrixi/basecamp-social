namespace BasecampSocial.Api.Models.Auth;

/// <summary>Request body for POST /api/v1/auth/login.</summary>
public sealed record LoginRequest(
    string UserName,
    string Password);
