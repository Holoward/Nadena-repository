using Application.Interfaces;
using Application.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Models;

namespace WebApi.Controllers.v1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // Validate password confirmation
        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new ServiceResponse<string>("Passwords do not match."));
        }

        var response = await _authService.RegisterAsync(
            request.FullName,
            request.Email,
            request.Password,
            request.Role,
            request.PayPalEmail,
            request.CommentCountEstimate,
            request.ContentTypes,
            request.YouTubeAccountAge,
            request.CompanyName,
            request.UseCase);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var response = await _authService.LoginAsync(request.Email, request.Password);

        if (!response.Success)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var response = await _authService.ForgotPasswordAsync(request.Email);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var response = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
    {
        var response = await _authService.VerifyEmailAsync(request.Email, request.Token);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
