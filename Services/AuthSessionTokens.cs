using Signal_CRUD_Demo_Backend.Contracts;

namespace Signal_CRUD_Demo_Backend.Services;

public sealed record AuthSessionTokens(
    AuthUserResponse User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
