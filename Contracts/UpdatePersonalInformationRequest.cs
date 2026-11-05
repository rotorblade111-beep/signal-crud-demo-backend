using System.ComponentModel.DataAnnotations;

namespace Signal_CRUD_Demo_Backend.Contracts;

/// <summary>Personal information to save for the authenticated account.</summary>
public sealed record UpdatePersonalInformationRequest
{
    /// <summary>The optional first name.</summary>
    [StringLength(100)]
    public string? FirstName { get; init; }

    /// <summary>The optional last name.</summary>
    [StringLength(100)]
    public string? LastName { get; init; }

    /// <summary>The account holder's date of birth.</summary>
    public DateOnly DateOfBirth { get; init; }

    /// <summary>The optional mailing address.</summary>
    [StringLength(500)]
    public string? Address { get; init; }

    /// <summary>The optional country.</summary>
    [StringLength(100)]
    public string? Country { get; init; }
}
