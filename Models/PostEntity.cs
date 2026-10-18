namespace Signal_CRUD_Demo_Backend.Models;

public sealed class PostEntity
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
