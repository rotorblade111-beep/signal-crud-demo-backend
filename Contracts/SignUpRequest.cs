using System.ComponentModel.DataAnnotations;

namespace Signal_CRUD_Demo_Backend.Contracts;

/// <summary>Payload used to register a new account.</summary>
public sealed record SignUpRequest
{
    /// <summary>The account email address.</summary>
    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    /// <summary>The account password.</summary>
    [Required, MinLength(8), StringLength(128)]
    public required string Password { get; init; }

    /// <summary>The account holder's date of birth.</summary>
    [Required]
    public DateOnly DateOfBirth { get; init; }
}
