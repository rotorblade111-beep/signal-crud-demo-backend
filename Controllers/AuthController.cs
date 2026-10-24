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
    [ProducesResponseType<AuthTokenResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthTokenResponse>> SignUp(
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

        var session = await authService.CreateSessionAsync(user, cancellationToken);
        SetRefreshTokenCookie(session);
        return StatusCode(StatusCodes.Status201Created, ToTokenResponse(session));
    }

    /// <summary>Verifies an account's login credentials.</summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenResponse>> Login(
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

        var session = await authService.CreateSessionAsync(user, cancellationToken);
        SetRefreshTokenCookie(session);
        return Ok(ToTokenResponse(session));
    }

    /// <summary>Rotates a refresh token and returns a fresh access token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokenResponse>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            ClearRefreshTokenCookie();
            return Unauthorized();
        }

        var session = await authService.RefreshSessionAsync(refreshToken, cancellationToken);
        if (session is null)
        {
            ClearRefreshTokenCookie();
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid refresh token",
                Detail = "The refresh token is invalid or expired."
            });
        }

        SetRefreshTokenCookie(session);
        return Ok(ToTokenResponse(session));
    }

    /// <summary>Revokes the current refresh token and ends its session.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.RevokeRefreshTokenAsync(
            Request.Cookies[RefreshTokenCookieName],
            cancellationToken);
        ClearRefreshTokenCookie();
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

    private const string RefreshTokenCookieName = "SignalCrudDemo.Refresh";

    private void SetRefreshTokenCookie(AuthSessionTokens session)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            session.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/auth",
                Expires = session.RefreshTokenExpiresAt
            });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            RefreshTokenCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/auth"
            });
    }

    private static AuthTokenResponse ToTokenResponse(AuthSessionTokens session) =>
        new(session.User, session.AccessToken, session.AccessTokenExpiresAt);
}
