using Microsoft.AspNetCore.Identity;
using UserService.Application.Abstractions;
using UserService.Domain.Entities;
using AspNetPasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace UserService.Infrastructure.Auth;

/// <summary>
/// Wraps ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/> (PBKDF2 with a
/// per-password salt) so the rest of the app depends only on the app-defined
/// <see cref="IPasswordHasher"/> abstraction, not on Identity's types.
/// </summary>
public class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _identityHasher = new();

    public string HashPassword(User user, string password) =>
        _identityHasher.HashPassword(user, password);

    public Application.Abstractions.PasswordVerificationResult VerifyPassword(User user, string providedPassword)
    {
        var result = _identityHasher.VerifyHashedPassword(user, user.PasswordHash, providedPassword);

        return result switch
        {
            AspNetPasswordVerificationResult.Success => Application.Abstractions.PasswordVerificationResult.Success,
            AspNetPasswordVerificationResult.SuccessRehashNeeded => Application.Abstractions.PasswordVerificationResult.SuccessRehashNeeded,
            _ => Application.Abstractions.PasswordVerificationResult.Failed
        };
    }
}
