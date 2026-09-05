using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserService.Application.Dtos;
using Xunit;

namespace UserService.IntegrationTests;

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static RegisterRequest NewRegisterRequest(string? email = null) => new()
    {
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = email ?? $"ada-{Guid.NewGuid():N}@example.com",
        Password = "SuperSecret1"
    };

    [Fact]
    public async Task Register_WithValidData_Returns201AndProfileWithoutPasswordHash()
    {
        var request = NewRegisterRequest();

        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal(request.Email.ToLowerInvariant(), profile!.Email);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var request = NewRegisterRequest();

        var first = await _client.PostAsJsonAsync("/api/users/register", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/users/register", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400()
    {
        var request = NewRegisterRequest();
        request.Email = "not-an-email";

        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/users/login", new LoginRequest
        {
            Email = $"nobody-{Guid.NewGuid():N}@example.com",
            Password = "WrongPassword1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwt()
    {
        var registerRequest = NewRegisterRequest();
        await _client.PostAsJsonAsync("/api/users/register", registerRequest);

        var response = await _client.PostAsJsonAsync("/api/users/login", new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.Equal(registerRequest.Email.ToLowerInvariant(), auth.User.Email);
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsOwnProfile()
    {
        var registerRequest = NewRegisterRequest();
        await _client.PostAsJsonAsync("/api/users/register", registerRequest);
        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.Equal(registerRequest.Email.ToLowerInvariant(), profile!.Email);
    }

    [Fact]
    public async Task UpdateMe_WithValidToken_UpdatesNameAndIsReflectedOnNextGet()
    {
        var registerRequest = NewRegisterRequest();
        await _client.PostAsJsonAsync("/api/users/register", registerRequest);
        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        using var updateRequest = new HttpRequestMessage(HttpMethod.Put, "/api/users/me")
        {
            Content = JsonContent.Create(new UpdateProfileRequest { FirstName = "Grace", LastName = "Hopper" })
        };
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var getResponse = await _client.SendAsync(getRequest);
        var profile = await getResponse.Content.ReadFromJsonAsync<UserProfileResponse>();

        Assert.Equal("Grace", profile!.FirstName);
        Assert.Equal("Hopper", profile.LastName);
    }
}
