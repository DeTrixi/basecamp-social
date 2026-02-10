namespace BasecampSocial.Api.Models.Messages;

/// <summary>Response for GET /api/v1/messages/:conversationId (paginated).</summary>
public sealed record MessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    byte[] EncryptedPayload,
    string MessageType,
    DateTimeOffset CreatedAt);
