using BasecampSocial.Api.Data;
using BasecampSocial.Api.Data.Entities;
using BasecampSocial.Api.Models.Messages;
using Microsoft.EntityFrameworkCore;

namespace BasecampSocial.Api.Services;

/// <summary>Manages encrypted message retrieval and read receipts.</summary>
public interface IMessageService
{
    Task<List<MessageResponse>> GetMessagesAsync(Guid currentUserId, Guid conversationId, DateTimeOffset? before, int limit);
    Task MarkAsReadAsync(Guid currentUserId, Guid conversationId, ReadMessagesRequest request);
}

public class MessageService : IMessageService
{
    private readonly AppDbContext _db;

    public MessageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<MessageResponse>> GetMessagesAsync(
        Guid currentUserId, Guid conversationId, DateTimeOffset? before, int limit)
    {
        // Verify membership
        var isMember = await _db.ConversationMembers
            .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == currentUserId);

        if (!isMember)
            throw new UnauthorizedAccessException("You are not a member of this conversation.");

        limit = Math.Clamp(limit, 1, 100);

        var query = _db.Messages
            .Where(m => m.ConversationId == conversationId);

        if (before.HasValue)
            query = query.Where(m => m.CreatedAt < before.Value);

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return messages.Select(m => new MessageResponse(
            Id: m.Id,
            ConversationId: m.ConversationId,
            SenderId: m.SenderId,
            EncryptedPayload: m.EncryptedPayload,
            MessageType: m.MessageType.ToString(),
            CreatedAt: m.CreatedAt)).ToList();
    }

    public async Task MarkAsReadAsync(Guid currentUserId, Guid conversationId, ReadMessagesRequest request)
    {
        // Verify membership
        var isMember = await _db.ConversationMembers
            .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == currentUserId);

        if (!isMember)
            throw new UnauthorizedAccessException("You are not a member of this conversation.");

        foreach (var messageId in request.MessageIds)
        {
            var receipt = await _db.MessageReceipts
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == currentUserId);

            if (receipt is null)
            {
                _db.MessageReceipts.Add(new MessageReceipt
                {
                    MessageId = messageId,
                    UserId = currentUserId,
                    DeliveredAt = DateTimeOffset.UtcNow,
                    ReadAt = DateTimeOffset.UtcNow
                });
            }
            else if (receipt.ReadAt is null)
            {
                receipt.ReadAt = DateTimeOffset.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
    }
}
