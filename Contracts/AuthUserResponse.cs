namespace Signal_CRUD_Demo_Backend.Contracts;

/// <summary>Safe account information returned after registration or login.</summary>
public sealed record AuthUserResponse(int Id, string Email, DateOnly DateOfBirth);
