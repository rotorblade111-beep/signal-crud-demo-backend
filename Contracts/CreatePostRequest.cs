using System.ComponentModel.DataAnnotations;

namespace Signal_CRUD_Demo_Backend.Contracts;

/// <summary>Payload used to create a post.</summary>
public sealed record CreatePostRequest
{
    /// <summary>The post title.</summary>
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Title { get; init; }

    /// <summary>The post body.</summary>
    [Required, StringLength(10000, MinimumLength = 1)]
    public required string Body { get; init; }
}
