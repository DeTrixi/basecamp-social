namespace BasecampSocial.Api.Models.Polls;

/// <summary>Request body for POST /api/v1/polls/:pollId/vote.</summary>
public sealed record VoteRequest(Guid PollOptionId, string Response); // "Yes" | "Maybe" | "No"
