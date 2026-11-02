using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Signal_CRUD_Demo_Backend.Contracts;
using Signal_CRUD_Demo_Backend.Services;

namespace Signal_CRUD_Demo_Backend.Controllers;

[ApiController]
[Authorize]
[Route("posts")]
public sealed class PostsController(IPostService postService) : ControllerBase
{
    /// <summary>Gets all posts, or a selected range when offset and limit are provided.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PostResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PostResponse>>> GetAll(
        [FromQuery] int? userId,
        [FromQuery] int? offset,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        if (userId is <= 0)
        {
            ModelState.AddModelError(nameof(userId), "User ID must be greater than zero.");
        }

        if (offset is null && limit is null)
        {
            return ModelState.IsValid
                ? Ok(await postService.GetAllAsync(userId, cancellationToken))
                : ValidationProblem(ModelState);
        }

        if (offset is null || limit is null)
        {
            ModelState.AddModelError("pagination", "Both offset and limit must be supplied together.");
            return ValidationProblem(ModelState);
        }

        if (offset < 0)
        {
            ModelState.AddModelError(nameof(offset), "Offset must be zero or greater.");
        }
        if (limit is < 1 or > 100)
        {
            ModelState.AddModelError(nameof(limit), "Limit must be between 1 and 100.");
        }
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return Ok(await postService.GetRangeAsync(
            userId,
            offset.Value,
            limit.Value,
            cancellationToken));
    }

    /// <summary>Gets a post by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PostResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var post = await postService.GetByIdAsync(id, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    /// <summary>Creates a post.</summary>
    [HttpPost]
    [ProducesResponseType<PostResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostResponse>> Create(
        CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var post = await postService.CreateAsync(userId.Value, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }

    /// <summary>Updates a post's title and body.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<PostResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostResponse>> Update(
        int id,
        UpdatePostRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var post = await postService.UpdateAsync(id, userId.Value, request, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    /// <summary>Deletes a post.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var deleted = await postService.DeleteAsync(id, userId.Value, cancellationToken);
        return deleted ? NoContent() : NotFound();
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
