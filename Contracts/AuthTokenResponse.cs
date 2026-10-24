namespace Signal_CRUD_Demo_Backend.Contracts;

/// <summary>Access token and account information returned after authentication.</summary>
public sealed record AuthTokenResponse(
    AuthUserResponse User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt);
