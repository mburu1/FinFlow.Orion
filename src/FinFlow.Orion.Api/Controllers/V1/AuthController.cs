using FinFlow.Orion.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

using ApiLoginRequest = FinFlow.Orion.Api.Models.Auth.LoginRequest;
using ApiLoginResponse = FinFlow.Orion.Api.Models.Auth.LoginResponse;
using ApiRegisterRequest = FinFlow.Orion.Api.Models.Auth.RegisterRequest;
using ApiRefreshTokenRequest = FinFlow.Orion.Api.Models.Auth.RefreshTokenRequest;
using ApiRefreshTokenResponse = FinFlow.Orion.Api.Models.Auth.RefreshTokenResponse;

namespace FinFlow.Orion.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    // =========================================================================
    // REGISTER
    // =========================================================================

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Register(
        [FromBody] ApiRegisterRequest request)
    {
        try
        {
            var user = await _userService.RegisterAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                HttpContext.RequestAborted);

            return Ok(new
            {
                message = "User registered successfully.",
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // =========================================================================
    // LOGIN
    // =========================================================================

    [HttpPost("login")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(ApiLoginResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiLoginResponse>> Login(
        [FromBody] ApiLoginRequest request)
    {
        try
        {
            var result = await _userService.LoginAsync(
                request.Email,
                request.Password,
                HttpContext.RequestAborted);

            return Ok(new ApiLoginResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken.Token,
                ExpiresAt = result.RefreshToken.ExpiresAt,
                Email = result.User.Email,
                FirstName = result.User.FirstName,
                LastName = result.User.LastName
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    // =========================================================================
    // REFRESH TOKEN
    // =========================================================================

    [HttpPost("refresh")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(ApiRefreshTokenResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiRefreshTokenResponse>> Refresh(
        [FromBody] ApiRefreshTokenRequest request)
    {
        try
        {
            var result = await _userService.RefreshTokenAsync(
                request.RefreshToken,
                HttpContext.RequestAborted);

            return Ok(new ApiRefreshTokenResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken.Token,
                ExpiresAt = result.RefreshToken.ExpiresAt
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    // =========================================================================
    // LOGOUT
    // =========================================================================

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        return Ok(new
        {
            message = "Logged out successfully"
        });
    }
}