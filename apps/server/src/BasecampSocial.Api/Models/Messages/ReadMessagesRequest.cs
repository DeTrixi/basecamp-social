namespace BasecampSocial.Api.Models.Messages;

/// <summary>Request body for POST /api/v1/messages/:conversationId/read.</summary>
public sealed record ReadMessagesRequest(List<Guid> MessageIds);
