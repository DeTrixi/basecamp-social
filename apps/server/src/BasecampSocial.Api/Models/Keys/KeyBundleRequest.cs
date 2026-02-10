namespace BasecampSocial.Api.Models.Keys;

/// <summary>Request body for POST /api/v1/keys/bundle (upload pre-key bundle).</summary>
public sealed record KeyBundleRequest(
    byte[] IdentityKey,
    byte[] SignedPreKey,
    byte[] SignedPreKeySignature,
    List<byte[]> OneTimePreKeys);
