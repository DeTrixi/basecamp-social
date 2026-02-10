namespace BasecampSocial.Api.Data.Entities;

/// <summary>
/// Lifecycle states for a date poll.
/// - Open: members can vote, creator can add options.
/// - Finalized: a date has been chosen and locked. Votes are preserved for reference.
/// - Closed: poll is archived (cancelled or expired via ClosesAt deadline).
/// </summary>
public enum PollStatus
{
    Open,
    Finalized,
    Closed
}

/// <summary>
/// A Doodle-style date poll within a conversation, allowing group members to
/// vote on proposed dates/times to find the best option.
/// 
/// Design decisions:
/// - Polls are scoped to a <see cref="Conversation"/> because they are a group
///   coordination feature ("When should we go climbing?"). They appear inline
///   in the chat, similar to how WhatsApp or Telegram handle polls.
/// - <see cref="Title"/> is required (e.g. "Weekend climbing trip") while
///   <see cref="Description"/> is optional for additional context.
/// - <see cref="Status"/> follows a simple state machine: Open → Finalized or
///   Open → Closed. Once finalized, the <see cref="ChosenOptionId"/> is set to
///   the winning date/time option.
/// - <see cref="ChosenOptionId"/> is nullable — only set when the poll creator
///   finalizes the poll by picking a date. This is a manual action, not automatic,
///   because the "best" date may involve nuance beyond just vote counts.
/// - <see cref="ClosesAt"/> enables optional auto-close deadlines. A background
///   job or lazy check can transition the status to Closed when this time passes.
/// - Poll content (title, options, votes) is NOT encrypted because polls are a
///   coordination feature where all group members need to see the options and
///   results. If E2EE for polls is needed later, the entire poll could be
///   encrypted as a special message type.
/// </summary>
public class Poll
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid CreatedBy { get; set; }

    /// <summary>Short title for the poll, e.g. "Weekend climbing trip".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description with additional context.</summary>
    public string? Description { get; set; }

    /// <summary>Current lifecycle state: Open, Finalized, or Closed.</summary>
    public PollStatus Status { get; set; } = PollStatus.Open;

    /// <summary>Set when finalized — references the chosen PollOption.</summary>
    public Guid? ChosenOptionId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Optional auto-close deadline. Null = no deadline.</summary>
    public DateTimeOffset? ClosesAt { get; set; }

    // Navigation
    public Conversation Conversation { get; set; } = null!;
    public AppUser Creator { get; set; } = null!;
    public PollOption? ChosenOption { get; set; }
    public ICollection<PollOption> Options { get; set; } = [];
}
