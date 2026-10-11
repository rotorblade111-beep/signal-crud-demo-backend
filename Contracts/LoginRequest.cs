using System.ComponentModel.DataAnnotations;

namespace Signal_CRUD_Demo_Backend.Contracts;

/// <summary>Credentials used to log in to an account.</summary>
public sealed record LoginRequest
{
    /// <summary>The account email address used as the username.</summary>
    [Required, EmailAddress, StringLength(254)]
    public required string Username { get; init; }

    /// <summary>The account password.</summary>
    [Required, StringLength(128)]
    public required string Password { get; init; }
}
