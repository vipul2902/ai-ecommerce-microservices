using Moq;
using UserService.Application.Abstractions;
using UserService.Application.Common;
using UserService.Application.Dtos;
using UserService.Application.Users;
using UserService.Domain.Entities;
using Xunit;
using PasswordVerificationResult = UserService.Application.Abstractions.PasswordVerificationResult;

namespace UserService.UnitTests.Users;

public class UserAccountServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly UserAccountService _sut;

    public UserAccountServiceTests()
    {
        _sut = new UserAccountService(_userRepository.Object, _passwordHasher.Object, _jwtTokenGenerator.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_CreatesUserWithHashedPasswordAndNoRawPasswordStored()
    {
        var request = new RegisterRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "Ada@Example.com",
            Password = "SuperSecret1"
        };

        _userRepository.Setup(r => r.EmailExistsAsync("ada@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasher.Setup(h => h.HashPassword(It.IsAny<User>(), request.Password))
            .Returns("hashed-value");

        User? capturedUser = null;
        _userRepository.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .Returns(Task.CompletedTask);

        var result = await _sut.RegisterAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal("ada@example.com", result.Value!.Email);
        Assert.Equal(Roles.Customer, result.Value.Role);

        Assert.NotNull(capturedUser);
        Assert.Equal("hashed-value", capturedUser!.PasswordHash);
        Assert.NotEqual(request.Password, capturedUser.PasswordHash);

        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ReturnsConflictAndDoesNotCreateUser()
    {
        var request = new RegisterRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Password = "SuperSecret1"
        };

        _userRepository.Setup(r => r.EmailExistsAsync("ada@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.RegisterAsync(request);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Conflict, result.Error!.Type);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ReturnsUnauthorized()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("missing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.LoginAsync(new LoginRequest { Email = "missing@example.com", Password = "whatever1" });

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Unauthorized, result.Error!.Type);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsUnauthorizedAndDoesNotIssueToken()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "ada@example.com", PasswordHash = "stored-hash" };
        _userRepository.Setup(r => r.GetByEmailAsync("ada@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword(user, "wrong-password"))
            .Returns(PasswordVerificationResult.Failed);

        var result = await _sut.LoginAsync(new LoginRequest { Email = "ada@example.com", Password = "wrong-password" });

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Unauthorized, result.Error!.Type);
        _jwtTokenGenerator.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndProfileWithoutPasswordHash()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            PasswordHash = "stored-hash",
            Role = Roles.Customer
        };

        _userRepository.Setup(r => r.GetByEmailAsync("ada@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword(user, "correct-password"))
            .Returns(PasswordVerificationResult.Success);

        var expiresAt = DateTime.UtcNow.AddHours(1);
        _jwtTokenGenerator.Setup(j => j.GenerateToken(user))
            .Returns(new GeneratedToken("jwt-token", expiresAt));

        var result = await _sut.LoginAsync(new LoginRequest { Email = "ada@example.com", Password = "correct-password" });

        Assert.True(result.Succeeded);
        Assert.Equal("jwt-token", result.Value!.Token);
        Assert.Equal(user.Email, result.Value.User.Email);

        var serialized = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("stored-hash", serialized);
        Assert.DoesNotContain("PasswordHash", serialized);
    }

    [Fact]
    public async Task LoginAsync_WhenHasherFlagsRehashNeeded_ReHashesAndPersistsBeforeIssuingToken()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "ada@example.com", PasswordHash = "old-hash" };
        _userRepository.Setup(r => r.GetByEmailAsync("ada@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword(user, "correct-password"))
            .Returns(PasswordVerificationResult.SuccessRehashNeeded);
        _passwordHasher.Setup(h => h.HashPassword(user, "correct-password"))
            .Returns("new-hash");
        _jwtTokenGenerator.Setup(j => j.GenerateToken(user))
            .Returns(new GeneratedToken("jwt-token", DateTime.UtcNow.AddHours(1)));

        var result = await _sut.LoginAsync(new LoginRequest { Email = "ada@example.com", Password = "correct-password" });

        Assert.True(result.Succeeded);
        Assert.Equal("new-hash", user.PasswordHash);
        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_ForUnknownUserId_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.GetProfileAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesNamesOnlyAndLeavesEmailAndPasswordUntouched()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Old",
            LastName = "Name",
            Email = "ada@example.com",
            PasswordHash = "unchanged-hash",
            Role = Roles.Customer
        };
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.UpdateProfileAsync(user.Id, new UpdateProfileRequest { FirstName = "New", LastName = "Name2" });

        Assert.True(result.Succeeded);
        Assert.Equal("New", user.FirstName);
        Assert.Equal("Name2", user.LastName);
        Assert.Equal("ada@example.com", user.Email);
        Assert.Equal("unchanged-hash", user.PasswordHash);
    }
}
