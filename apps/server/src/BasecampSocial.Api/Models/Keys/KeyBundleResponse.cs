namespace BasecampSocial.Api.Models.Keys;

/// <summary>Response body for GET /api/v1/keys/:userId/bundle.</summary>
public sealed record KeyBundleResponse(
    Guid Id,
    Guid UserId,
    byte[] IdentityKey,
    byte[] SignedPreKey,
    byte[] SignedPreKeySignature,
    List<byte[]> OneTimePreKeys,
    DateTimeOffset CreatedAt);
