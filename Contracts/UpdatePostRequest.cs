using System.ComponentModel.DataAnnotations;

namespace Signal_CRUD_Demo_Backend.Contracts;

/// <summary>Payload used to update a post title and body.</summary>
public sealed record UpdatePostRequest
{
    /// <summary>The updated post title.</summary>
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Title { get; init; }

    /// <summary>The updated post body.</summary>
    [Required, StringLength(10000, MinimumLength = 1)]
    public required string Body { get; init; }
}
