using Microsoft.AspNetCore.Identity;

namespace BasecampSocial.Api.Data.Entities;

/// <summary>
/// Represents a registered user in the Basecamp Social system.
/// 
/// Design decisions:
/// - Extends <see cref="IdentityUser{Guid}"/> to leverage ASP.NET Core Identity for
///   password hashing (Argon2id), account lockout, email/phone confirmation, and
///   two-factor auth — all battle-tested rather than hand-rolled.
/// - Uses <c>Guid</c> as the primary key instead of the default <c>string</c> because:
///   (1) GUIDs are globally unique, enabling future multi-region/federated scenarios.
///   (2) They avoid sequential ID enumeration attacks.
///   (3) They are compatible with the UUID type in PostgreSQL for efficient indexing.
/// - <see cref="DisplayName"/> is separate from <c>UserName</c> (inherited from Identity)
///   because the username is used for login/lookup (immutable, unique), while the
///   display name is the user-facing label shown in chat (mutable, not necessarily unique).
/// - <see cref="AvatarUrl"/> stores a URL to an S3/MinIO object rather than the image
///   bytes directly, keeping the users table lightweight and avoiding large row sizes.
/// - <see cref="StatusMessage"/> allows users to set a short text status (e.g. "Climbing
///   this weekend!") visible on their profile — common in messaging apps.
/// - <see cref="LastSeenAt"/> tracks when the user was last active. Updated via SignalR
///   presence events. Nullable because a user who just registered has never been "seen".
/// - All timestamps use <see cref="DateTimeOffset"/> rather than <see cref="DateTime"/>
///   to preserve timezone information and avoid UTC conversion bugs. PostgreSQL stores
///   these as <c>timestamptz</c>.
/// - Navigation properties use the C# 12 collection expression <c>[]</c> syntax for
///   clean initialization. Each collection represents a different relationship the user
///   participates in across the system.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    /// <summary>User-facing name shown in conversations and profiles.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>URL to the user's avatar image stored in S3/MinIO.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Optional short status message visible on the user's profile.</summary>
    public string? StatusMessage { get; set; }

    /// <summary>When the account was created. Defaults to UTC now.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Last time the user was online. Updated via SignalR presence tracking.</summary>
    public DateTimeOffset? LastSeenAt { get; set; }

    // Navigation properties
    public ICollection<KeyBundle> KeyBundles { get; set; } = [];
    public ICollection<ConversationMember> ConversationMemberships { get; set; } = [];
    public ICollection<Message> SentMessages { get; set; } = [];
    public ICollection<MessageReceipt> MessageReceipts { get; set; } = [];
    public ICollection<Poll> CreatedPolls { get; set; } = [];
    public ICollection<PollVote> PollVotes { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<DevicePushToken> DevicePushTokens { get; set; } = [];
}
