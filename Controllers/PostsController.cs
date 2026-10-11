using Microsoft.AspNetCore.Mvc;
using Signal_CRUD_Demo_Backend.Contracts;
using Signal_CRUD_Demo_Backend.Services;

namespace Signal_CRUD_Demo_Backend.Controllers;

[ApiController]
[Route("posts")]
public sealed class PostsController(IPostService postService) : ControllerBase
{
    /// <summary>Gets all posts.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PostResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PostResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await postService.GetAllAsync(cancellationToken));

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
        var post = await postService.CreateAsync(request, cancellationToken);
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
        var post = await postService.UpdateAsync(id, request, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    /// <summary>Deletes a post.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await postService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
