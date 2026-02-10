namespace BasecampSocial.Api.Models.Conversations;

/// <summary>Response for conversation list and detail endpoints.</summary>
public sealed record ConversationResponse(
    Guid Id,
    string Type,
    string? Name,
    string? AvatarUrl,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    List<ConversationMemberResponse> Members);

/// <summary>A member within a conversation response.</summary>
public sealed record ConversationMemberResponse(
    Guid UserId,
    string UserName,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    DateTimeOffset JoinedAt);
