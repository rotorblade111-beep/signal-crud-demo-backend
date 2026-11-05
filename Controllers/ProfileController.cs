using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Signal_CRUD_Demo_Backend.Contracts;
using Signal_CRUD_Demo_Backend.Services;

namespace Signal_CRUD_Demo_Backend.Controllers;

[ApiController]
[Authorize]
[Route("profile")]
public sealed class ProfileController(IProfileService profileService) : ControllerBase
{
    /// <summary>Gets the authenticated user's profile.</summary>
    [HttpGet]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> Get(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var profile = await profileService.GetAsync(userId.Value, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>Updates the authenticated user's personal information.</summary>
    [HttpPut("personal-information")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> UpdatePersonalInformation(
        UpdatePersonalInformationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await profileService.UpdatePersonalInformationAsync(
            userId.Value,
            request,
            cancellationToken);
        if (result == ProfileUpdateResult.NotFound)
        {
            return NotFound();
        }
        if (result == ProfileUpdateResult.InvalidDateOfBirth)
        {
            ModelState.AddModelError(
                nameof(request.DateOfBirth),
                "Date of birth must be a valid date that is not in the future.");
            return ValidationProblem(ModelState);
        }

        var profile = await profileService.GetAsync(userId.Value, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>Updates the authenticated user's email address.</summary>
    [HttpPut("email")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> UpdateEmail(
        UpdateProfileEmailRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await profileService.UpdateEmailAsync(userId.Value, request, cancellationToken);
        if (result == ProfileUpdateResult.NotFound)
        {
            return NotFound();
        }
        if (result == ProfileUpdateResult.EmailConflict)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Email already in use",
                Detail = "Another account already uses this email address."
            });
        }

        var profile = await profileService.GetAsync(userId.Value, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>Changes the authenticated user's password.</summary>
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        ChangeProfilePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var changed = await profileService.ChangePasswordAsync(userId.Value, request, cancellationToken);
        return changed ? NoContent() : NotFound();
    }

    private int? CurrentUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
            && userId > 0
                ? userId
                : null;
    }
}
