using UserService.Domain.Entities;

namespace UserService.Application.Abstractions;

public record GeneratedToken(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    GeneratedToken GenerateToken(User user);
}
