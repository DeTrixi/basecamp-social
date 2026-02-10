namespace BasecampSocial.Api.Data.Entities;

/// <summary>
/// Stores the public key material a user uploads so that other users can initiate
/// an E2EE session with them — even if they are offline.
/// 
/// Design decisions:
/// - This implements the server-side storage for the X3DH (Extended Triple
///   Diffie-Hellman) key agreement protocol, modelled after the Signal Protocol.
/// - <see cref="IdentityKey"/>: The user's long-term public identity key (Ed25519).
///   This never changes for a given device and is used to verify the user's identity.
/// - <see cref="SignedPreKey"/>: A medium-term public key (X25519) that rotates
///   periodically (e.g. weekly). Signed by the identity key to prove authenticity.
/// - <see cref="SignedPreKeySignature"/>: The Ed25519 signature over the signed
///   pre-key, allowing the initiator to verify it hasn't been tampered with.
/// - <see cref="OneTimePreKeys"/>: A batch of single-use public keys. Each is
///   consumed when another user initiates a session, providing forward secrecy for
///   the initial message. Stored as a PostgreSQL <c>BYTEA[]</c> array. When depleted,
///   the client must upload a fresh batch.
/// - All keys are stored as raw <c>byte[]</c> (BYTEA in PostgreSQL) rather than
///   Base64 strings to avoid encoding/decoding overhead and to match the binary
///   nature of cryptographic keys.
/// - The server stores ONLY public keys. Private keys never leave the user's device
///   (stored in iOS Keychain / Android Keystore). The server is zero-knowledge.
/// </summary>
public class KeyBundle
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Long-term public identity key (Ed25519). Used to verify the user's identity.</summary>
    public byte[] IdentityKey { get; set; } = [];

    /// <summary>Medium-term public key (X25519) for key agreement. Rotates periodically.</summary>
    public byte[] SignedPreKey { get; set; } = [];

    /// <summary>Ed25519 signature over the SignedPreKey, proving it belongs to this identity.</summary>
    public byte[] SignedPreKeySignature { get; set; } = [];

    /// <summary>Batch of single-use public keys consumed during session initiation (forward secrecy).</summary>
    public List<byte[]> OneTimePreKeys { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public AppUser User { get; set; } = null!;
}
