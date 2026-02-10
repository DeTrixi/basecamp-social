namespace BasecampSocial.Api.Models.Conversations;

/// <summary>Request body for PATCH /api/v1/conversations/:id.</summary>
public sealed record UpdateConversationRequest(
    string? Name,
    string? AvatarUrl);
