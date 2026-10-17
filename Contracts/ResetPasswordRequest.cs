using System.ComponentModel.DataAnnotations;

namespace Signal_CRUD_Demo_Backend.Contracts;

/// <summary>Payload used to replace an account password.</summary>
public sealed record ResetPasswordRequest
{
    /// <summary>The account email address.</summary>
    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    /// <summary>The new account password.</summary>
    [Required, MinLength(8), StringLength(128)]
    public required string NewPassword { get; init; }
}
