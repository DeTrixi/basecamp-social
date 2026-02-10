namespace BasecampSocial.Api.Data.Entities;

/// <summary>
/// A proposed date/time option within a poll that members can vote on.
/// 
/// Design decisions:
/// - Each option represents a specific time slot, e.g. "Saturday 9 AM – 12 PM".
/// - <see cref="StartsAt"/> is always required (when does this option begin?).
///   <see cref="EndsAt"/> is optional — some events are point-in-time ("meet at 9")
///   while others have a duration ("9 AM to noon").
/// - <see cref="Label"/> provides an optional human-readable description like
///   "Saturday morning" or "After work". This is displayed alongside the date/time
///   in the UI for context.
/// - <see cref="SortOrder"/> controls display ordering in the poll. Options are
///   shown in this order rather than sorted by date, because the creator may want
///   to present them in a specific logical sequence.
/// - Uses a surrogate Guid key rather than a composite key because options need
///   to be individually referenced (e.g. <see cref="Poll.ChosenOptionId"/> points
///   to one specific option).
/// </summary>
public class PollOption
{
    public Guid Id { get; set; }
    public Guid PollId { get; set; }

    /// <summary>When this proposed time slot starts.</summary>
    public DateTimeOffset StartsAt { get; set; }

    /// <summary>Optional end time. Null for point-in-time events.</summary>
    public DateTimeOffset? EndsAt { get; set; }

    /// <summary>Optional label, e.g. "Saturday morning". Shown alongside the date in the UI.</summary>
    public string? Label { get; set; }

    /// <summary>Display ordering within the poll (not necessarily chronological).</summary>
    public int SortOrder { get; set; }

    // Navigation
    public Poll Poll { get; set; } = null!;
    public ICollection<PollVote> Votes { get; set; } = [];
}
