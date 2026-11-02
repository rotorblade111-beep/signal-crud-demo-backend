using Microsoft.EntityFrameworkCore;
using Signal_CRUD_Demo_Backend.Contracts;
using Signal_CRUD_Demo_Backend.Data;
using Signal_CRUD_Demo_Backend.Models;

namespace Signal_CRUD_Demo_Backend.Services;

public sealed class PostService(AppDbContext dbContext) : IPostService
{
    public async Task<IReadOnlyList<PostResponse>> GetAllAsync(
        int? userId,
        CancellationToken cancellationToken) =>
        await dbContext.Posts
            .AsNoTracking()
            .Where(post => userId == null || post.UserId == userId)
            .OrderBy(post => post.Id)
            .Select(post => new PostResponse(
                post.Id,
                post.Title,
                post.Body,
                post.CreatedAt,
                post.UserId,
                post.User == null ? null : post.User.Email))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PostResponse>> GetRangeAsync(
        int? userId,
        int offset,
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.Posts
            .AsNoTracking()
            .Where(post => userId == null || post.UserId == userId)
            .OrderBy(post => post.Id)
            .Skip(offset)
            .Take(limit)
            .Select(post => new PostResponse(
                post.Id,
                post.Title,
                post.Body,
                post.CreatedAt,
                post.UserId,
                post.User == null ? null : post.User.Email))
            .ToListAsync(cancellationToken);

    public async Task<PostResponse?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.Posts
            .AsNoTracking()
            .Where(post => post.Id == id)
            .Select(post => new PostResponse(
                post.Id,
                post.Title,
                post.Body,
                post.CreatedAt,
                post.UserId,
                post.User == null ? null : post.User.Email))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PostResponse> CreateAsync(
        int userId,
        CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleAsync(
            candidate => candidate.Id == userId,
            cancellationToken);
        var post = new PostEntity
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UserId = userId,
            User = user
        };

        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(post);
    }

    public async Task<PostResponse?> UpdateAsync(
        int id,
        int userId,
        UpdatePostRequest request,
        CancellationToken cancellationToken)
    {
        var post = await dbContext.Posts
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id && candidate.UserId == userId,
                cancellationToken);
        if (post is null)
        {
            return null;
        }

        post.Title = request.Title.Trim();
        post.Body = request.Body.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(post);
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken)
    {
        var post = await dbContext.Posts.SingleOrDefaultAsync(
            candidate => candidate.Id == id && candidate.UserId == userId,
            cancellationToken);
        if (post is null)
        {
            return false;
        }

        dbContext.Posts.Remove(post);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PostResponse ToResponse(PostEntity post) =>
        new(post.Id, post.Title, post.Body, post.CreatedAt, post.UserId, post.User?.Email);
}
