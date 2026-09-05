using UserService.Domain.Entities;

namespace UserService.Application.Abstractions;

public enum PasswordVerificationResult
{
    Failed,
    Success,

    /// <summary>Password is correct, but was hashed with outdated settings and should be re-hashed.</summary>
    SuccessRehashNeeded
}

public interface IPasswordHasher
{
    string HashPassword(User user, string password);

    PasswordVerificationResult VerifyPassword(User user, string providedPassword);
}
