using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Signal_CRUD_Demo_Backend.Contracts;
using Signal_CRUD_Demo_Backend.Data;
using Signal_CRUD_Demo_Backend.Models;

namespace Signal_CRUD_Demo_Backend.Services;

public sealed class ProfileService(
    AppDbContext dbContext,
    IPasswordHasher<UserEntity> passwordHasher) : IProfileService
{
    public async Task<ProfileResponse?> GetAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(candidate => candidate.Profile)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        return user is null ? null : ToResponse(user);
    }

    public async Task<ProfileUpdateResult> UpdatePersonalInformationAsync(
        int userId,
        UpdatePersonalInformationRequest request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(candidate => candidate.Profile)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return ProfileUpdateResult.NotFound;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.DateOfBirth == default || request.DateOfBirth > today)
        {
            return ProfileUpdateResult.InvalidDateOfBirth;
        }

        user.DateOfBirth = request.DateOfBirth;
        var profile = user.Profile ?? new UserProfileEntity { User = user };
        profile.FirstName = NormalizeOptional(request.FirstName);
        profile.LastName = NormalizeOptional(request.LastName);
        profile.Address = NormalizeOptional(request.Address);
        profile.Country = NormalizeOptional(request.Country);
        if (user.Profile is null)
        {
            user.Profile = profile;
            dbContext.UserProfiles.Add(profile);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ProfileUpdateResult.Updated;
    }

    public async Task<ProfileUpdateResult> UpdateEmailAsync(
        int userId,
        UpdateProfileEmailRequest request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken);
        if (user is null)
        {
            return ProfileUpdateResult.NotFound;
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(
            candidate => candidate.Id != userId && candidate.Email == email,
            cancellationToken))
        {
            return ProfileUpdateResult.EmailConflict;
        }

        user.Email = email;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            return ProfileUpdateResult.EmailConflict;
        }

        return ProfileUpdateResult.Updated;
    }

    public async Task<bool> ChangePasswordAsync(
        int userId,
        ChangeProfilePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProfileResponse ToResponse(UserEntity user) =>
        new(
            user.Id,
            user.Email,
            user.DateOfBirth,
            user.Profile?.FirstName,
            user.Profile?.LastName,
            user.Profile?.Address,
            user.Profile?.Country);
}
