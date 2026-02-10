using BasecampSocial.Api.Data;
using BasecampSocial.Api.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BasecampSocial.Api.Hubs;

/// <summary>
/// SignalR hub for real-time messaging, presence, and typing indicators.
/// All message payloads are encrypted — the server relays opaque blobs.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(AppDbContext db, ILogger<ChatHub> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Called when a client connects. Joins all conversation groups.</summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();

        // Add user to all their conversation groups for broadcasting
        var conversationIds = await _db.ConversationMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.ConversationId.ToString())
            .ToListAsync();

        foreach (var conversationId in conversationIds)
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

        // Notify presence
        await Clients.Others.SendAsync("PresenceChanged", new
        {
            UserId = userId,
            Status = "Online",
            LastSeen = DateTimeOffset.UtcNow
        });

        _logger.LogInformation("User {UserId} connected to ChatHub", userId);
        await base.OnConnectedAsync();
    }

    /// <summary>Called when a client disconnects.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();

        // Update last seen
        var user = await _db.Users.FindAsync(userId);
        if (user is not null)
        {
            user.LastSeenAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }

        // Notify presence
        await Clients.Others.SendAsync("PresenceChanged", new
        {
            UserId = userId,
            Status = "Offline",
            LastSeen = DateTimeOffset.UtcNow
        });

        _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client sends an encrypted message. Server persists and relays to conversation members.
    /// </summary>
    public async Task SendMessage(Guid conversationId, byte[] encryptedPayload, string clientMessageId, string messageType = "Text")
    {
        var userId = GetUserId();

        // Verify membership
        var isMember = await _db.ConversationMembers
            .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);

        if (!isMember)
        {
            await Clients.Caller.SendAsync("Error", "You are not a member of this conversation.");
            return;
        }

        // Persist the encrypted message
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = userId,
            EncryptedPayload = encryptedPayload,
            MessageType = Enum.Parse<MessageType>(messageType),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        // Relay to all members in the conversation group
        await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", new
        {
            ConversationId = conversationId,
            message.EncryptedPayload,
            SenderId = userId,
            Timestamp = message.CreatedAt,
            ServerMessageId = message.Id,
            ClientMessageId = clientMessageId
        });

        // Confirm delivery to the sender
        await Clients.Caller.SendAsync("MessageDelivered", new
        {
            ConversationId = conversationId,
            MessageId = message.Id,
            ClientMessageId = clientMessageId
        });

        _logger.LogDebug("Message {MessageId} sent in conversation {ConversationId}", message.Id, conversationId);
    }

    /// <summary>Client indicates they've read messages in a conversation.</summary>
    public async Task MessageRead(Guid conversationId, Guid messageId)
    {
        var userId = GetUserId();

        await Clients.OthersInGroup(conversationId.ToString()).SendAsync("MessageRead", new
        {
            ConversationId = conversationId,
            MessageId = messageId,
            ReadBy = userId
        });
    }

    /// <summary>Client starts typing in a conversation.</summary>
    public async Task StartTyping(Guid conversationId)
    {
        var userId = GetUserId();
        await Clients.OthersInGroup(conversationId.ToString()).SendAsync("TypingUpdate", new
        {
            ConversationId = conversationId,
            UserId = userId,
            IsTyping = true
        });
    }

    /// <summary>Client stops typing in a conversation.</summary>
    public async Task StopTyping(Guid conversationId)
    {
        var userId = GetUserId();
        await Clients.OthersInGroup(conversationId.ToString()).SendAsync("TypingUpdate", new
        {
            ConversationId = conversationId,
            UserId = userId,
            IsTyping = false
        });
    }

    /// <summary>Client signals they are online.</summary>
    public async Task SetOnline()
    {
        var userId = GetUserId();
        await Clients.Others.SendAsync("PresenceChanged", new
        {
            UserId = userId,
            Status = "Online",
            LastSeen = DateTimeOffset.UtcNow
        });
    }

    private Guid GetUserId()
    {
        var sub = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value
            ?? throw new HubException("User ID claim not found.");

        return Guid.Parse(sub);
    }
}
