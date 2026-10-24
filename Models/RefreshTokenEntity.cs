namespace Signal_CRUD_Demo_Backend.Models;

public sealed class RefreshTokenEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
