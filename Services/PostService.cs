using Microsoft.EntityFrameworkCore;
using Signal_CRUD_Demo_Backend.Contracts;
using Signal_CRUD_Demo_Backend.Data;
using Signal_CRUD_Demo_Backend.Models;

namespace Signal_CRUD_Demo_Backend.Services;

public sealed class PostService(AppDbContext dbContext) : IPostService
{
    public async Task<IReadOnlyList<PostResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Posts
            .AsNoTracking()
            .OrderBy(post => post.Id)
            .Select(post => new PostResponse(post.Id, post.Title, post.Body))
            .ToListAsync(cancellationToken);

    public async Task<PostResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.Posts
            .AsNoTracking()
            .Where(post => post.Id == id)
            .Select(post => new PostResponse(post.Id, post.Title, post.Body))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PostResponse> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken)
    {
        var post = new PostEntity
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim()
        };

        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(post);
    }

    public async Task<PostResponse?> UpdateAsync(
        int id,
        UpdatePostRequest request,
        CancellationToken cancellationToken)
    {
        var post = await dbContext.Posts.FindAsync([id], cancellationToken);
        if (post is null)
        {
            return null;
        }

        post.Title = request.Title.Trim();
        post.Body = request.Body.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(post);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var post = await dbContext.Posts.FindAsync([id], cancellationToken);
        if (post is null)
        {
            return false;
        }

        dbContext.Posts.Remove(post);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PostResponse ToResponse(PostEntity post) =>
        new(post.Id, post.Title, post.Body);
}
