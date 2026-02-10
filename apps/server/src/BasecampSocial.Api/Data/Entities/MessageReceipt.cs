namespace BasecampSocial.Api.Data.Entities;

/// <summary>
/// Tracks delivery and read status of a message for a specific recipient.
/// 
/// Design decisions:
/// - Uses a composite primary key <c>(MessageId, UserId)</c> — one receipt per
///   recipient per message. In a group chat with 50 members, a single message
///   generates up to 49 receipt rows (excluding the sender).
/// - <see cref="DeliveredAt"/> is set when the message reaches the recipient's
///   device (via SignalR acknowledgment or next poll). Null = not yet delivered.
/// - <see cref="ReadAt"/> is set when the recipient's client reports that the
///   message was displayed on screen. Null = not yet read. Read receipts are
///   optional and can be disabled per-user in the client.
/// - Both timestamps are nullable with independent semantics: a message can be
///   delivered but not read, and theoretically read without a delivery timestamp
///   (e.g. if the user fetches history on a new device).
/// - This design allows the sender to see double-check marks (delivered) and
///   blue checks (read) — the standard messaging app UX pattern.
/// - For group chats, the sender can see aggregated read status (e.g. "read by 12
///   of 49") computed from these rows.
/// </summary>
public class MessageReceipt
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>When the message was delivered to this user's device. Null = pending.</summary>
    public DateTimeOffset? DeliveredAt { get; set; }

    /// <summary>When the user opened/viewed the message. Null = unread. Optional feature.</summary>
    public DateTimeOffset? ReadAt { get; set; }

    // Navigation
    public Message Message { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
