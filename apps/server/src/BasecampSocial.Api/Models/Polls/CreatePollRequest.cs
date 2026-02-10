namespace BasecampSocial.Api.Models.Polls;

/// <summary>Request body for POST /api/v1/polls (create a date poll).</summary>
public sealed record CreatePollRequest(
    Guid ConversationId,
    string Title,
    string? Description,
    DateTimeOffset? ClosesAt,
    List<CreatePollOptionRequest> Options);

/// <summary>A single date/time option within a poll creation request.</summary>
public sealed record CreatePollOptionRequest(
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    string? Label);
