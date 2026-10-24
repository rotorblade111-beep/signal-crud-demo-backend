using Signal_CRUD_Demo_Backend.Contracts;

namespace Signal_CRUD_Demo_Backend.Services;

public interface IAuthService
{
    Task<AuthUserResponse?> RegisterAsync(SignUpRequest request, CancellationToken cancellationToken);
    Task<AuthUserResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthSessionTokens> CreateSessionAsync(AuthUserResponse user, CancellationToken cancellationToken);
    Task<AuthSessionTokens?> RefreshSessionAsync(string refreshToken, CancellationToken cancellationToken);
    Task RevokeRefreshTokenAsync(string? refreshToken, CancellationToken cancellationToken);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}
