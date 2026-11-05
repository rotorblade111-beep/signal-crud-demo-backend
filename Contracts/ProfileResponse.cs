namespace Signal_CRUD_Demo_Backend.Contracts;

/// <summary>Profile information for the authenticated account.</summary>
public sealed record ProfileResponse(
    int Id,
    string Email,
    DateOnly DateOfBirth,
    string? FirstName,
    string? LastName,
    string? Address,
    string? Country);
