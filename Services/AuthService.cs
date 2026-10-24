using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Signal_CRUD_Demo_Backend.Contracts;
using Signal_CRUD_Demo_Backend.Data;
using Signal_CRUD_Demo_Backend.Models;

namespace Signal_CRUD_Demo_Backend.Services;

public sealed class AuthService(
    AppDbContext dbContext,
    IPasswordHasher<UserEntity> passwordHasher,
    IAccessTokenService accessTokenService) : IAuthService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

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

    public async Task<AuthSessionTokens> CreateSessionAsync(
        AuthUserResponse user,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var refreshToken = CreateRefreshToken();
        var refreshTokenExpiresAt = now.Add(RefreshTokenLifetime);
        dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = HashRefreshToken(refreshToken),
            ExpiresAt = refreshTokenExpiresAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var (accessToken, accessTokenExpiresAt) = accessTokenService.CreateToken(user);
        return new AuthSessionTokens(
            user,
            accessToken,
            accessTokenExpiresAt,
            refreshToken,
            refreshTokenExpiresAt);
    }

    public async Task<AuthSessionTokens?> RefreshSessionAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var now = DateTimeOffset.UtcNow;
        var storedToken = await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        if (storedToken is null || storedToken.RevokedAt is not null || storedToken.ExpiresAt <= now)
        {
            return null;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var revoked = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "RefreshTokens"
            SET "RevokedAt" = {now}
            WHERE "Id" = {storedToken.Id}
                AND "RevokedAt" IS NULL
                AND julianday("ExpiresAt") > julianday({now});
            """,
            cancellationToken);
        if (revoked != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == storedToken.UserId, cancellationToken);
        if (user is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var response = ToResponse(user);
        var newRefreshToken = CreateRefreshToken();
        var newRefreshTokenExpiresAt = now.Add(RefreshTokenLifetime);
        dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = HashRefreshToken(newRefreshToken),
            ExpiresAt = newRefreshTokenExpiresAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var (accessToken, accessTokenExpiresAt) = accessTokenService.CreateToken(response);
        return new AuthSessionTokens(
            response,
            accessToken,
            accessTokenExpiresAt,
            newRefreshToken,
            newRefreshTokenExpiresAt);
    }

    public async Task RevokeRefreshTokenAsync(
        string? refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = HashRefreshToken(refreshToken);
        var now = DateTimeOffset.UtcNow;
        await dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, now),
                cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == email,
            cancellationToken);

        if (user is null)
        {
            return false;
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string CreateRefreshToken() =>
        Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));

    private static string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private static AuthUserResponse ToResponse(UserEntity user) =>
        new(user.Id, user.Email, user.DateOfBirth);
}
