namespace BasecampSocial.Api.Models.Polls;

/// <summary>Response for poll detail endpoints.</summary>
public sealed record PollResponse(
    Guid Id,
    Guid ConversationId,
    Guid CreatedBy,
    string Title,
    string? Description,
    string Status,
    Guid? ChosenOptionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosesAt,
    List<PollOptionResponse> Options);

/// <summary>A single poll option with vote counts.</summary>
public sealed record PollOptionResponse(
    Guid Id,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    string? Label,
    int SortOrder,
    List<PollVoteResponse> Votes);

/// <summary>A single vote on a poll option.</summary>
public sealed record PollVoteResponse(
    Guid UserId,
    string UserName,
    string Response,
    DateTimeOffset VotedAt);
