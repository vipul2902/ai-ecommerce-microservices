using UserService.Application.Abstractions;
using UserService.Domain.Entities;
using UserService.Infrastructure.Auth;
using Xunit;

namespace UserService.UnitTests.Auth;

public class PasswordHasherAdapterTests
{
    private readonly PasswordHasherAdapter _sut = new();

    [Fact]
    public void HashPassword_NeverReturnsThePlaintextPassword()
    {
        var user = new User { Id = Guid.NewGuid() };

        var hash = _sut.HashPassword(user, "SuperSecret1");

        Assert.NotEqual("SuperSecret1", hash);
        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_Succeeds()
    {
        var user = new User { Id = Guid.NewGuid() };
        user.PasswordHash = _sut.HashPassword(user, "SuperSecret1");

        var result = _sut.VerifyPassword(user, "SuperSecret1");

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_Fails()
    {
        var user = new User { Id = Guid.NewGuid() };
        user.PasswordHash = _sut.HashPassword(user, "SuperSecret1");

        var result = _sut.VerifyPassword(user, "TotallyWrongPassword1");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void HashPassword_ProducesDifferentHashesForTheSamePassword_DueToRandomSalt()
    {
        var user = new User { Id = Guid.NewGuid() };

        var hash1 = _sut.HashPassword(user, "SuperSecret1");
        var hash2 = _sut.HashPassword(user, "SuperSecret1");

        Assert.NotEqual(hash1, hash2);
    }
}
