using Signal_CRUD_Demo_Backend.Contracts;

namespace Signal_CRUD_Demo_Backend.Services;

public interface IAccessTokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateToken(AuthUserResponse user);
}
