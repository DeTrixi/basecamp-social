namespace BasecampSocial.Api.Data.Entities;

/// <summary>
/// The three possible responses to a poll option, matching Doodle's model.
/// - Yes: "I can make it" (green check in UI).
/// - Maybe: "I might be able to" (yellow question mark).
/// - No: "I can't make it" (red cross). Also the implicit default for non-voters.
/// </summary>
public enum VoteResponse
{
    Yes,
    Maybe,
    No
}

/// <summary>
/// A single user's vote on a single poll option.
/// 
/// Design decisions:
/// - Uses a composite primary key <c>(PollOptionId, UserId)</c> — one vote per
///   user per option. This means a user votes independently on each time slot
///   ("Yes to Saturday, Maybe to Sunday, No to Monday"), exactly like Doodle.
/// - <see cref="Response"/> uses a three-way enum (Yes/Maybe/No) rather than a
///   boolean because "Maybe" is essential for real-world scheduling — it lets
///   the group see which dates have the most flexibility.
/// - <see cref="VotedAt"/> tracks when the vote was cast or last updated. Users
///   can change their vote until the poll is finalized — an upsert on the
///   composite key handles this cleanly.
/// - The composite key also prevents duplicate votes at the database level,
///   eliminating the need for application-level deduplication logic.
/// - Vote counts per option are computed at query time (COUNT + GROUP BY)
///   rather than stored as a denormalized counter, because poll sizes are small
///   (max 1,000 members) and real-time accuracy matters more than read performance.
/// </summary>
public class PollVote
{
    public Guid PollOptionId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>The user's response: Yes, Maybe, or No.</summary>
    public VoteResponse Response { get; set; }

    /// <summary>When the vote was cast or last changed.</summary>
    public DateTimeOffset VotedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public PollOption PollOption { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
