namespace Signal_CRUD_Demo_Backend.Models;

public sealed class UserProfileEntity
{
    public int UserId { get; set; }
    public required UserEntity User { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
}
