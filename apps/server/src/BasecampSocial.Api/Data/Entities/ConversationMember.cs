namespace BasecampSocial.Api.Data.Entities;

/// <summary>
/// Roles within a conversation.
/// - Member: can send/receive messages and vote on polls.
/// - Admin: can also rename the group, change the avatar, add/remove members,
///   and manage polls. The conversation creator is automatically an Admin.
/// </summary>
public enum MemberRole
{
    Member,
    Admin
}

/// <summary>
/// Join table linking users to conversations with role and join timestamp.
/// 
/// Design decisions:
/// - Uses a composite primary key <c>(ConversationId, UserId)</c> rather than a
///   surrogate Guid. This enforces at the database level that a user can only be
///   a member of a given conversation once, and makes membership lookups efficient
///   (a single index covers both "all members of conversation X" and "is user Y
///   in conversation X" queries).
/// - <see cref="Role"/> controls permissions within the conversation. Kept simple
///   (Member/Admin) for the MVP — can be extended later (e.g. Moderator, ReadOnly).
/// - <see cref="JoinedAt"/> tracks when the user joined, useful for determining
///   which messages they should be able to see (e.g. don't show messages from
///   before they joined a group).
/// - This entity does NOT include a "left at" timestamp. When a user leaves,
///   the row is deleted and their Sender Keys are rotated for forward secrecy.
///   If historical membership tracking is needed later, a separate audit log
///   would be more appropriate.
/// </summary>
public class ConversationMember
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>The user's role in this conversation (Member or Admin).</summary>
    public MemberRole Role { get; set; } = MemberRole.Member;

    /// <summary>When the user joined. Used to scope visible message history.</summary>
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Conversation Conversation { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
