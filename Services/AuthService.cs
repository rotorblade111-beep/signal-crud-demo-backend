using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Signal_CRUD_Demo_Backend.Contracts;
using Signal_CRUD_Demo_Backend.Data;
using Signal_CRUD_Demo_Backend.Models;

namespace Signal_CRUD_Demo_Backend.Services;

public sealed class AuthService(
    AppDbContext dbContext,
    IPasswordHasher<UserEntity> passwordHasher) : IAuthService
{
    public async Task<AuthUserResponse?> RegisterAsync(
        SignUpRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return null;
        }

        var user = new UserEntity
        {
            Email = email,
            PasswordHash = string.Empty,
            DateOfBirth = request.DateOfBirth
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            return null;
        }

        return ToResponse(user);
    }

    public async Task<AuthUserResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Username);
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == email,
            cancellationToken);

        if (user is null)
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        return verification is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded
            ? ToResponse(user)
            : null;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static AuthUserResponse ToResponse(UserEntity user) =>
        new(user.Id, user.Email, user.DateOfBirth);
}
