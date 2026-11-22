using System.Security.Claims;
using CvBank.Api.Auth;
using CvBank.Api.Models;
using CvBank.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CvBank.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService, UserService userService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);

        if (result is null)
        {
            return Unauthorized(new { message = "Invalid user name or password." });
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<CurrentUserDto> Me()
    {
        return Ok(new CurrentUserDto(
            int.Parse(User.FindFirstValue(AuthClaims.UserId) ?? "0"),
            User.FindFirstValue(AuthClaims.UserName) ?? string.Empty,
            User.FindFirstValue(AuthClaims.FullName) ?? string.Empty,
            User.FindFirstValue(AuthClaims.Role) ?? string.Empty,
            User.FindAll(AuthClaims.Permission).Select(c => c.Value).Distinct().ToArray()));
    }

    /// Any signed-in user may replace their own password; the app signs the user out afterwards so the change is confirmed.
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        await userService.ChangeOwnPasswordAsync(SignedInUser.From(User), request);
        return NoContent();
    }
}
