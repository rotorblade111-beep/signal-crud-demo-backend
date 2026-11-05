using System.ComponentModel.DataAnnotations;

namespace Signal_CRUD_Demo_Backend.Contracts;

/// <summary>New password for the authenticated account.</summary>
public sealed record ChangeProfilePasswordRequest
{
    /// <summary>The new account password.</summary>
    [Required, MinLength(8), StringLength(128)]
    public required string NewPassword { get; init; }
}
