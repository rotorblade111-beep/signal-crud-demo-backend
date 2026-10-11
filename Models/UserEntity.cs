namespace Signal_CRUD_Demo_Backend.Models;

public sealed class UserEntity
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateOnly DateOfBirth { get; set; }
}
