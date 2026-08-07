using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Api.Models.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FinFlow.Orion.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var (user, accessToken, refreshToken) = await _userService.LoginAsync(request.Email, request.Password, HttpContext.RequestAborted);

            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = refreshToken.ExpiresAt,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var (user, accessToken, refreshToken) = await _userService.RefreshTokenAsync(request.RefreshToken, HttpContext.RequestAborted);

            return Ok(new RefreshTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = refreshToken.ExpiresAt
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("