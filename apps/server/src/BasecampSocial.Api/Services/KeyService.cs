using BasecampSocial.Api.Data;
using BasecampSocial.Api.Data.Entities;
using BasecampSocial.Api.Models.Keys;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BasecampSocial.Api.Services;

/// <summary>Manages E2EE pre-key bundle upload and retrieval.</summary>
public interface IKeyService
{
    Task<KeyBundleResponse> UploadBundleAsync(Guid userId, KeyBundleRequest request);
    Task<KeyBundleResponse> GetBundleAsync(Guid userId);
}

public class KeyService : IKeyService
{
    private readonly AppDbContext _db;
    private readonly IValidator<KeyBundleRequest> _validator;

    public KeyService(AppDbContext db, IValidator<KeyBundleRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<KeyBundleResponse> UploadBundleAsync(Guid userId, KeyBundleRequest request)
    {
        await _validator.ValidateAndThrowAsync(request);

        var bundle = new KeyBundle
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IdentityKey = request.IdentityKey,
            SignedPreKey = request.SignedPreKey,
            SignedPreKeySignature = request.SignedPreKeySignature,
            OneTimePreKeys = request.OneTimePreKeys,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.KeyBundles.Add(bundle);
        await _db.SaveChangesAsync();

        return MapToResponse(bundle);
    }

    public async Task<KeyBundleResponse> GetBundleAsync(Guid userId)
    {
        var bundle = await _db.KeyBundles
            .Where(kb => kb.UserId == userId)
            .OrderByDescending(kb => kb.CreatedAt)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"No key bundle found for user {userId}.");

        return MapToResponse(bundle);
    }

    private static KeyBundleResponse MapToResponse(KeyBundle kb) => new(
        Id: kb.Id,
        UserId: kb.UserId,
        IdentityKey: kb.IdentityKey,
        SignedPreKey: kb.SignedPreKey,
        SignedPreKeySignature: kb.SignedPreKeySignature,
        OneTimePreKeys: kb.OneTimePreKeys,
        CreatedAt: kb.CreatedAt);
}
