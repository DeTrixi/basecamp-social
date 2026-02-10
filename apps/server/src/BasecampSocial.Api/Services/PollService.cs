using BasecampSocial.Api.Data;
using BasecampSocial.Api.Data.Entities;
using BasecampSocial.Api.Models.Polls;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BasecampSocial.Api.Services;

/// <summary>Manages date poll CRUD, voting, and finalisation.</summary>
public interface IPollService
{
    Task<PollResponse> CreateAsync(Guid currentUserId, CreatePollRequest request);
    Task<PollResponse> GetByIdAsync(Guid currentUserId, Guid pollId);
    Task<PollResponse> VoteAsync(Guid currentUserId, Guid pollId, VoteRequest request);
    Task DeleteAsync(Guid currentUserId, Guid pollId);
    Task<PollResponse> FinalizeAsync(Guid currentUserId, Guid pollId, FinalizePollRequest request);
}

public class PollService : IPollService
{
    private readonly AppDbContext _db;
    private readonly IValidator<CreatePollRequest> _createValidator;
    private readonly IValidator<VoteRequest> _voteValidator;

    public PollService(
        AppDbContext db,
        IValidator<CreatePollRequest> createValidator,
        IValidator<VoteRequest> voteValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _voteValidator = voteValidator;
    }

    public async Task<PollResponse> CreateAsync(Guid currentUserId, CreatePollRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        // Verify the user is a member of the conversation
        var isMember = await _db.ConversationMembers
            .AnyAsync(cm => cm.ConversationId == request.ConversationId && cm.UserId == currentUserId);

        if (!isMember)
            throw new UnauthorizedAccessException("You are not a member of this conversation.");

        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            CreatedBy = currentUserId,
            Title = request.Title,
            Description = request.Description,
            Status = PollStatus.Open,
            ClosesAt = request.ClosesAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var sortOrder = 0;
        foreach (var opt in request.Options)
        {
            poll.Options.Add(new PollOption
            {
                Id = Guid.NewGuid(),
                PollId = poll.Id,
                StartsAt = opt.StartsAt,
                EndsAt = opt.EndsAt,
                Label = opt.Label,
                SortOrder = sortOrder++
            });
        }

        _db.Polls.Add(poll);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(currentUserId, poll.Id);
    }

    public async Task<PollResponse> GetByIdAsync(Guid currentUserId, Guid pollId)
    {
        var poll = await _db.Polls
            .Include(p => p.Options).ThenInclude(o => o.Votes).ThenInclude(v => v.User)
            .FirstOrDefaultAsync(p => p.Id == pollId)
            ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

        // Verify the user is a member of the conversation
        var isMember = await _db.ConversationMembers
            .AnyAsync(cm => cm.ConversationId == poll.ConversationId && cm.UserId == currentUserId);

        if (!isMember)
            throw new UnauthorizedAccessException("You are not a member of this conversation.");

        return MapToResponse(poll);
    }

    public async Task<PollResponse> VoteAsync(Guid currentUserId, Guid pollId, VoteRequest request)
    {
        await _voteValidator.ValidateAndThrowAsync(request);

        var poll = await _db.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == pollId)
            ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

        if (poll.Status != PollStatus.Open)
            throw new ArgumentException("This poll is no longer open for voting.");

        // Verify the option belongs to this poll
        var option = poll.Options.FirstOrDefault(o => o.Id == request.PollOptionId)
            ?? throw new ArgumentException("Option does not belong to this poll.");

        // Parse the response
        var response = Enum.Parse<VoteResponse>(request.Response);

        // Upsert the vote
        var existingVote = await _db.PollVotes
            .FirstOrDefaultAsync(v => v.PollOptionId == request.PollOptionId && v.UserId == currentUserId);

        if (existingVote is not null)
        {
            existingVote.Response = response;
            existingVote.VotedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            _db.PollVotes.Add(new PollVote
            {
                PollOptionId = request.PollOptionId,
                UserId = currentUserId,
                Response = response,
                VotedAt = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return await GetByIdAsync(currentUserId, pollId);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid pollId)
    {
        var poll = await _db.Polls.FindAsync(pollId)
            ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

        // Only the creator can delete
        if (poll.CreatedBy != currentUserId)
            throw new UnauthorizedAccessException("Only the poll creator can delete it.");

        poll.Status = PollStatus.Closed;
        await _db.SaveChangesAsync();
    }

    public async Task<PollResponse> FinalizeAsync(Guid currentUserId, Guid pollId, FinalizePollRequest request)
    {
        var poll = await _db.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == pollId)
            ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

        if (poll.CreatedBy != currentUserId)
            throw new UnauthorizedAccessException("Only the poll creator can finalize it.");

        if (poll.Status != PollStatus.Open)
            throw new ArgumentException("This poll is not open.");

        if (!poll.Options.Any(o => o.Id == request.ChosenOptionId))
            throw new ArgumentException("Chosen option does not belong to this poll.");

        poll.Status = PollStatus.Finalized;
        poll.ChosenOptionId = request.ChosenOptionId;
        await _db.SaveChangesAsync();

        return await GetByIdAsync(currentUserId, pollId);
    }

    private static PollResponse MapToResponse(Poll p) => new(
        Id: p.Id,
        ConversationId: p.ConversationId,
        CreatedBy: p.CreatedBy,
        Title: p.Title,
        Description: p.Description,
        Status: p.Status.ToString(),
        ChosenOptionId: p.ChosenOptionId,
        CreatedAt: p.CreatedAt,
        ClosesAt: p.ClosesAt,
        Options: p.Options.OrderBy(o => o.SortOrder).Select(o => new PollOptionResponse(
            Id: o.Id,
            StartsAt: o.StartsAt,
            EndsAt: o.EndsAt,
            Label: o.Label,
            SortOrder: o.SortOrder,
            Votes: o.Votes.Select(v => new PollVoteResponse(
                UserId: v.UserId,
                UserName: v.User.UserName!,
                Response: v.Response.ToString(),
                VotedAt: v.VotedAt)).ToList())).ToList());
}
