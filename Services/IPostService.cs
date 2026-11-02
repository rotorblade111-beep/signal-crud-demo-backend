using Signal_CRUD_Demo_Backend.Contracts;

namespace Signal_CRUD_Demo_Backend.Services;

public interface IPostService
{
    Task<IReadOnlyList<PostResponse>> GetAllAsync(
        int? userId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<PostResponse>> GetRangeAsync(
        int? userId,
        int offset,
        int limit,
        CancellationToken cancellationToken);
    Task<PostResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<PostResponse> CreateAsync(
        int userId,
        CreatePostRequest request,
        CancellationToken cancellationToken);
    Task<PostResponse?> UpdateAsync(
        int id,
        int userId,
        UpdatePostRequest request,
        CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken);
}
