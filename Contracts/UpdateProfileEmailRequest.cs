using System.ComponentModel.DataAnnotations;

namespace Signal_CRUD_Demo_Backend.Contracts;

/// <summary>Email address to use for the authenticated account.</summary>
public sealed record UpdateProfileEmailRequest
{
    /// <summary>The new account email address.</summary>
    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }
}
