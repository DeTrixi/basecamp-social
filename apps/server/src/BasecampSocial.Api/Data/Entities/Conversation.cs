namespace BasecampSocial.Api.Data.Entities;

/// <summary>
/// Distinguishes between 1-on-1 and group conversations.
/// - Direct: exactly 2 members, no name/avatar (the UI shows the other user's info).
/// - Group: 2–1,000 members with a shared name and optional avatar.
/// </summary>
public enum ConversationType
{
    Direct,
    Group
}

/// <summary>
/// Represents a chat conversation — either a 1-on-1 direct message or a group chat.
/// 
/// Design decisions:
/// - A single <c>Conversation</c> entity handles both direct and group chats rather
///   than having two separate tables. This simplifies the message and membership
///   models — a message always belongs to one conversation regardless of type.
/// - <see cref="Type"/> determines the behavior: Direct conversations have exactly
///   2 members and null Name/AvatarUrl; Group conversations have a name, optional
///   avatar, and up to 1,000 members.
/// - <see cref="Name"/> is nullable because direct conversations don't have a name —
///   the client shows the other participant's DisplayName instead.
/// - <see cref="CreatedBy"/> tracks who initiated the conversation for auditing and
///   to assign the initial Admin role in group chats.
/// - Polls are scoped to a conversation (not global) because date polls are a group
///   coordination feature — e.g. "When should the climbing club meet this weekend?"
/// - Messages are navigable from the conversation, but in practice the API uses
///   cursor-based pagination (conversation_id + created_at DESC) rather than
///   loading the full collection, to handle conversations with thousands of messages.
/// </summary>
public class Conversation
{
    public Guid Id { get; set; }

    /// <summary>Whether this is a 1-on-1 (Direct) or multi-user (Group) conversation.</summary>
    public ConversationType Type { get; set; }

    /// <summary>Group name. Null for direct conversations (UI shows other user's name).</summary>
    public string? Name { get; set; }

    /// <summary>Group avatar URL. Null for direct conversations.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>The user who created this conversation. Gets Admin role in groups.</summary>
    public Guid CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public AppUser Creator { get; set; } = null!;
    public ICollection<ConversationMember> Members { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<Poll> Polls { get; set; } = [];
}
