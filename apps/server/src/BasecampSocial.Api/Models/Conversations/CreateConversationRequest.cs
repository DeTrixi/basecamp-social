namespace BasecampSocial.Api.Models.Conversations;

/// <summary>Request body for POST /api/v1/conversations (create conversation).</summary>
public sealed record CreateConversationRequest(
    string Type,           // "Direct" or "Group"
    string? Name,          // Required for Group, null for Direct
    List<Guid> MemberIds); // Other user IDs to include
