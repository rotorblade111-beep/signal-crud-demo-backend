using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Signal_CRUD_Demo_Backend.Contracts;
using Signal_CRUD_Demo_Backend.Services;

namespace Signal_CRUD_Demo_Backend.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Registers an account.</summary>
    [HttpPost("sign-up")]
    [ProducesResponseType<AuthUserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthUserResponse>> SignUp(
        SignUpRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(request, cancellationToken);
        if (user is null)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Account already exists",
                Detail = "An account with this email address already exists."
            });
        }

        await SignInUserAsync(user);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    /// <summary>Verifies an account's login credentials.</summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authService.LoginAsync(request, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid credentials",
                Detail = "The username or password is incorrect."
            });
        }

        await SignInUserAsync(user);
        return Ok(user);
    }

    /// <summary>Returns the account associated with the current persistent session.</summary>
    [Authorize]
    [HttpGet("session")]
    [ProducesResponseType<AuthUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<AuthUserResponse> GetSession()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var dateOfBirthValue = User.FindFirstValue(ClaimTypes.DateOfBirth);

        if (!int.TryParse(idValue, out var id) ||
            string.IsNullOrWhiteSpace(email) ||
            !DateOnly.TryParseExact(
                dateOfBirthValue,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateOfBirth))
        {
            return Unauthorized();
        }

        return Ok(new AuthUserResponse(id, email, dateOfBirth));
    }

    /// <summary>Ends the current persistent session.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>Replaces the password for an existing account.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var reset = await authService.ResetPasswordAsync(request, cancellationToken);
        if (!reset)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Account not found",
                Detail = "No account was found for that email address."
            });
        }

        return NoContent();
    }

    private Task SignInUserAsync(AuthUserResponse user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.DateOfBirth, user.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);
    }
}
