using Signal_CRUD_Demo_Backend.Contracts;

namespace Signal_CRUD_Demo_Backend.Services;

public interface IPostService
{
    Task<IReadOnlyList<PostResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<PostResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<PostResponse> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken);
    Task<PostResponse?> UpdateAsync(int id, UpdatePostRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
