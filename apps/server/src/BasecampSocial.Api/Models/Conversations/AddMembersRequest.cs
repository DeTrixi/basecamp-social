namespace BasecampSocial.Api.Models.Conversations;

/// <summary>Request body for POST /api/v1/conversations/:id/members.</summary>
public sealed record AddMembersRequest(List<Guid> UserIds);
