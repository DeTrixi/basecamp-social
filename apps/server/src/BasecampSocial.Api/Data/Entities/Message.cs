namespace BasecampSocial.Api.Data.Entities;

/// <summary>
/// The type of content in the message. The server doesn't inspect the encrypted
/// payload, but this hint allows the client to know what kind of content to
/// expect before decryption (e.g. show a placeholder image frame vs text bubble).
/// </summary>
public enum MessageType
{
    Text,
    Image,
    File
}

/// <summary>
/// Represents an encrypted message stored on the server. The server is zero-knowledge —
/// it sees only opaque ciphertext and metadata.
/// 
/// Design decisions:
/// - <see cref="EncryptedPayload"/> is a <c>byte[]</c> (BYTEA in PostgreSQL) containing
///   the full ciphertext blob: encrypted message content + AES-GCM nonce + ratchet
///   header. The server cannot read, search, or index the content — by design.
/// - <see cref="MessageType"/> is stored in plaintext as a UX hint. This is a deliberate
///   trade-off: it leaks minimal metadata ("this is an image") but allows the client
///   to render appropriate placeholders before decryption completes. If stricter
///   metadata protection is needed, this could be moved inside the encrypted payload.
/// - <see cref="SenderId"/> is stored in plaintext because the server needs to know
///   who sent the message for delivery routing, presence, and receipts. This is
///   consistent with Signal's approach — sender identity is metadata, not content.
/// - <see cref="CreatedAt"/> is server-assigned (not client-provided) to prevent
///   timestamp manipulation and ensure consistent ordering.
/// - The table is indexed on <c>(ConversationId, CreatedAt DESC)</c> for efficient
///   cursor-based pagination when fetching message history.
/// - There is a 64 KB soft limit for text messages and 25 MB for media (enforced
///   at the API layer, not the database). Media files themselves are stored in S3;
///   the encrypted payload here would contain the encrypted S3 key + URL.
/// </summary>
public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }

    /// <summary>Opaque encrypted blob: ciphertext + nonce + ratchet header. Server cannot read this.</summary>
    public byte[] EncryptedPayload { get; set; } = [];

    /// <summary>UX hint for the client (stored in plaintext). See class docs for trade-off rationale.</summary>
    public MessageType MessageType { get; set; } = MessageType.Text;

    /// <summary>Server-assigned timestamp for consistent ordering. Indexed for pagination.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Conversation Conversation { get; set; } = null!;
    public AppUser Sender { get; set; } = null!;
    public ICollection<MessageReceipt> Receipts { get; set; } = [];
}
