namespace Signal_CRUD_Demo_Backend.Contracts;

/// <summary>Represents a post returned by the API.</summary>
public sealed record PostResponse(int Id, string Title, string Body, DateTimeOffset CreatedAt);
