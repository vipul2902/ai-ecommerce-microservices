using UserService.Application.Common;
using UserService.Application.Dtos;

namespace UserService.Application.Users;

public interface IUserAccountService
{
    Task<ServiceResult<UserProfileResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<UserProfileResponse>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ServiceResult<UserProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
