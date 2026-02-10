namespace BasecampSocial.Api.Models.Polls;

/// <summary>Request body for PATCH /api/v1/polls/:pollId/finalize.</summary>
public sealed record FinalizePollRequest(Guid ChosenOptionId);
