using System.Security.Claims;
using BasecampSocial.Api.Data;
using BasecampSocial.Api.Data.Entities;
using BasecampSocial.Api.Models.Users;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BasecampSocial.Api.Services;

/// <summary>Manages user profile reads and updates.</summary>
public interface IUserService
{
    Task<UserProfileResponse> GetProfileAsync(Guid userId);
    Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<List<UserProfileResponse>> SearchUsersAsync(string query);
}

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IValidator<UpdateProfileRequest> _updateValidator;

    public UserService(
        UserManager<AppUser> userManager,
        AppDbContext db,
        IValidator<UpdateProfileRequest> updateValidator)
    {
        _userManager = userManager;
        _db = db;
        _updateValidator = updateValidator;
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        return MapToResponse(user);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        await _updateValidator.ValidateAndThrowAsync(request);

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (request.DisplayName is not null) user.DisplayName = request.DisplayName;
        if (request.AvatarUrl is not null) user.AvatarUrl = request.AvatarUrl;
        if (request.StatusMessage is not null) user.StatusMessage = request.StatusMessage;

        await _userManager.UpdateAsync(user);
        return MapToResponse(user);
    }

    public async Task<List<UserProfileResponse>> SearchUsersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return [];

        var normalised = query.ToLower();

        var users = await _db.Users
            .Where(u => u.UserName!.ToLower().Contains(normalised)
                     || u.DisplayName.ToLower().Contains(normalised))
            .Take(20)
            .ToListAsync();

        return users.Select(MapToResponse).ToList();
    }

    private static UserProfileResponse MapToResponse(AppUser user) => new(
        Id: user.Id,
        UserName: user.UserName!,
        DisplayName: user.DisplayName,
        AvatarUrl: user.AvatarUrl,
        StatusMessage: user.StatusMessage,
        CreatedAt: user.CreatedAt,
        LastSeenAt: user.LastSeenAt);
}
