using Signal_CRUD_Demo_Backend.Contracts;

namespace Signal_CRUD_Demo_Backend.Services;

public enum ProfileUpdateResult
{
    Updated,
    NotFound,
    EmailConflict,
    InvalidDateOfBirth
}

public interface IProfileService
{
    Task<ProfileResponse?> GetAsync(int userId, CancellationToken cancellationToken);
    Task<ProfileUpdateResult> UpdatePersonalInformationAsync(
        int userId,
        UpdatePersonalInformationRequest request,
        CancellationToken cancellationToken);
    Task<ProfileUpdateResult> UpdateEmailAsync(
        int userId,
        UpdateProfileEmailRequest request,
        CancellationToken cancellationToken);
    Task<bool> ChangePasswordAsync(
        int userId,
        ChangeProfilePasswordRequest request,
        CancellationToken cancellationToken);
}
