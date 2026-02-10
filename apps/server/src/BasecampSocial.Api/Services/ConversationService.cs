using BasecampSocial.Api.Data;
using BasecampSocial.Api.Data.Entities;
using BasecampSocial.Api.Models.Conversations;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BasecampSocial.Api.Services;

/// <summary>Manages conversation CRUD and membership.</summary>
public interface IConversationService
{
    Task<ConversationResponse> CreateAsync(Guid currentUserId, CreateConversationRequest request);
    Task<List<ConversationResponse>> ListAsync(Guid currentUserId);
    Task<ConversationResponse> GetByIdAsync(Guid currentUserId, Guid conversationId);
    Task<ConversationResponse> UpdateAsync(Guid currentUserId, Guid conversationId, UpdateConversationRequest request);
    Task AddMembersAsync(Guid currentUserId, Guid conversationId, AddMembersRequest request);
    Task RemoveMemberAsync(Guid currentUserId, Guid conversationId, Guid memberUserId);
}

public class ConversationService : IConversationService
{
    private readonly AppDbContext _db;
    private readonly IValidator<CreateConversationRequest> _createValidator;

    public ConversationService(AppDbContext db, IValidator<CreateConversationRequest> createValidator)
    {
        _db = db;
        _createValidator = createValidator;
    }

    public async Task<ConversationResponse> CreateAsync(Guid currentUserId, CreateConversationRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        var type = Enum.Parse<ConversationType>(request.Type);

        // For direct conversations, check if one already exists between the two users
        if (type == ConversationType.Direct)
        {
            var otherUserId = request.MemberIds[0];
            var existing = await _db.ConversationMembers
                .Where(cm => cm.UserId == currentUserId)
                .Select(cm => cm.Conversation)
                .Where(c => c.Type == ConversationType.Direct)
                .Where(c => c.Members.Any(m => m.UserId == otherUserId))
                .FirstOrDefaultAsync();

            if (existing is not null)
                return await GetByIdAsync(currentUserId, existing.Id);
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = type,
            Name = request.Name,
            CreatedBy = currentUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Add the creator as admin
        conversation.Members.Add(new ConversationMember
        {
            ConversationId = conversation.Id,
            UserId = currentUserId,
            Role = MemberRole.Admin,
            JoinedAt = DateTimeOffset.UtcNow
        });

        // Add requested members
        foreach (var memberId in request.MemberIds)
        {
            conversation.Members.Add(new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = memberId,
                Role = MemberRole.Member,
                JoinedAt = DateTimeOffset.UtcNow
            });
        }

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(currentUserId, conversation.Id);
    }

    public async Task<List<ConversationResponse>> ListAsync(Guid currentUserId)
    {
        var conversations = await _db.Conversations
            .Where(c => c.Members.Any(m => m.UserId == currentUserId))
            .Include(c => c.Members).ThenInclude(m => m.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return conversations.Select(MapToResponse).ToList();
    }

    public async Task<ConversationResponse> GetByIdAsync(Guid currentUserId, Guid conversationId)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} not found.");

        // Ensure the user is a member
        if (!conversation.Members.Any(m => m.UserId == currentUserId))
            throw new UnauthorizedAccessException("You are not a member of this conversation.");

        return MapToResponse(conversation);
    }

    public async Task<ConversationResponse> UpdateAsync(Guid currentUserId, Guid conversationId, UpdateConversationRequest request)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} not found.");

        var membership = conversation.Members.FirstOrDefault(m => m.UserId == currentUserId)
            ?? throw new UnauthorizedAccessException("You are not a member of this conversation.");

        if (membership.Role != MemberRole.Admin)
            throw new UnauthorizedAccessException("Only admins can update the conversation.");

        if (request.Name is not null) conversation.Name = request.Name;
        if (request.AvatarUrl is not null) conversation.AvatarUrl = request.AvatarUrl;

        await _db.SaveChangesAsync();
        return MapToResponse(conversation);
    }

    public async Task AddMembersAsync(Guid currentUserId, Guid conversationId, AddMembersRequest request)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} not found.");

        if (!conversation.Members.Any(m => m.UserId == currentUserId && m.Role == MemberRole.Admin))
            throw new UnauthorizedAccessException("Only admins can add members.");

        if (conversation.Type == ConversationType.Direct)
            throw new ArgumentException("Cannot add members to a direct conversation.");

        foreach (var userId in request.UserIds)
        {
            if (conversation.Members.Any(m => m.UserId == userId))
                continue;

            conversation.Members.Add(new ConversationMember
            {
                ConversationId = conversationId,
                UserId = userId,
                Role = MemberRole.Member,
                JoinedAt = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Guid currentUserId, Guid conversationId, Guid memberUserId)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} not found.");

        var currentMembership = conversation.Members.FirstOrDefault(m => m.UserId == currentUserId)
            ?? throw new UnauthorizedAccessException("You are not a member of this conversation.");

        // Users can remove themselves; admins can remove anyone
        if (currentUserId != memberUserId && currentMembership.Role != MemberRole.Admin)
            throw new UnauthorizedAccessException("Only admins can remove other members.");

        var target = conversation.Members.FirstOrDefault(m => m.UserId == memberUserId)
            ?? throw new KeyNotFoundException("User is not a member of this conversation.");

        _db.ConversationMembers.Remove(target);
        await _db.SaveChangesAsync();
    }

    private static ConversationResponse MapToResponse(Conversation c) => new(
        Id: c.Id,
        Type: c.Type.ToString(),
        Name: c.Name,
        AvatarUrl: c.AvatarUrl,
        CreatedBy: c.CreatedBy,
        CreatedAt: c.CreatedAt,
        Members: c.Members.Select(m => new ConversationMemberResponse(
            UserId: m.UserId,
            UserName: m.User.UserName!,
            DisplayName: m.User.DisplayName,
            AvatarUrl: m.User.AvatarUrl,
            Role: m.Role.ToString(),
            JoinedAt: m.JoinedAt)).ToList());
}
