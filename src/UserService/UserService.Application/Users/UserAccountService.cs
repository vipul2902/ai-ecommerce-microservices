using UserService.Application.Abstractions;
using UserService.Application.Common;
using UserService.Application.Dtos;
using UserService.Domain.Entities;

namespace UserService.Application.Users;

public class UserAccountService : IUserAccountService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public UserAccountService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<ServiceResult<UserProfileResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return ServiceResult<UserProfileResponse>.Failure(
                ServiceErrorType.Conflict,
                "An account with this email address already exists.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            Role = Roles.Customer,
            CreatedAt = now,
            UpdatedAt = now
        };

        // PasswordHash is set after the user object exists because the hasher's
        // salted output depends on the user, not just the plaintext password.
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<UserProfileResponse>.Success(ToProfileResponse(user));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            return ServiceResult<AuthResponse>.Failure(
                ServiceErrorType.Unauthorized,
                "Invalid email or password.");
        }

        var verificationResult = _passwordHasher.VerifyPassword(user, request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return ServiceResult<AuthResponse>.Failure(
                ServiceErrorType.Unauthorized,
                "Invalid email or password.");
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return ServiceResult<AuthResponse>.Success(new AuthResponse
        {
            Token = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = ToProfileResponse(user)
        });
    }

    public async Task<ServiceResult<UserProfileResponse>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        return user is null
            ? ServiceResult<UserProfileResponse>.Failure(ServiceErrorType.NotFound, "User not found.")
            : ServiceResult<UserProfileResponse>.Success(ToProfileResponse(user));
    }

    public async Task<ServiceResult<UserProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return ServiceResult<UserProfileResponse>.Failure(ServiceErrorType.NotFound, "User not found.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<UserProfileResponse>.Success(ToProfileResponse(user));
    }

    private static UserProfileResponse ToProfileResponse(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}
