using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Signal_CRUD_Demo_Backend.Contracts;

namespace Signal_CRUD_Demo_Backend.Services;

public sealed class AccessTokenService : IAccessTokenService
{
    public const string Issuer = "SignalCrudDemo";
    public const string Audience = "SignalCrudDemoFrontend";
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(1);

    private readonly SigningCredentials signingCredentials;

    public AccessTokenService(string signingKey)
    {
        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public (string Token, DateTimeOffset ExpiresAt) CreateToken(AuthUserResponse user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(TokenLifetime);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.DateOfBirth, user.DateOfBirth.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
