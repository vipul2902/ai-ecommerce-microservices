using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Common;
using UserService.Application.Dtos;
using UserService.Application.Users;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserAccountService _userAccountService;

    public UsersController(IUserAccountService userAccountService)
    {
        _userAccountService = userAccountService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserProfileResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _userAccountService.RegisterAsync(request, cancellationToken);

        return result.Succeeded
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : MapError(result.Error!);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _userAccountService.LoginAsync(request, cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : MapError(result.Error!);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> GetMe(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _userAccountService.GetProfileAsync(userId.Value, cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : MapError(result.Error!);
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMe(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _userAccountService.UpdateProfileAsync(userId.Value, request, cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : MapError(result.Error!);
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idClaim, out var id) ? id : null;
    }

    private ActionResult MapError(ServiceError error) => error.Type switch
    {
        ServiceErrorType.Validation => BadRequest(new { message = error.Message }),
        ServiceErrorType.Conflict => Conflict(new { message = error.Message }),
        ServiceErrorType.Unauthorized => Unauthorized(new { message = error.Message }),
        ServiceErrorType.NotFound => NotFound(new { message = error.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." })
    };
}
