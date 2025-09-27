using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly IActivityLogService _activityLogService;

    public CommentsController(ICommentService commentService, IActivityLogService activityLogService)
    {
        _commentService = commentService;
        _activityLogService = activityLogService;
    }

    [HttpGet("todo/{todoId:int}")]
    public async Task<IActionResult> GetCommentsForTodo(int todoId)
    {
        var comments = await _commentService.GetCommentsForTodoAsync(todoId);
        return Ok(comments);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetComment(int id)
    {
        var comment = await _commentService.GetCommentByIdAsync(id);
        return Ok(comment);
    }

    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] CommentCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var comment = await _commentService.CreateCommentAsync(request);
        return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, comment);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateComment(int id, [FromBody] CommentUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _commentService.UpdateCommentAsync(id, request);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        await _commentService.DeleteCommentAsync(id);
        return NoContent();
    }

    [HttpGet("todo/{todoId:int}/activity")]
    public async Task<IActionResult> GetActivity(int todoId)
    {
        var activity = await _activityLogService.GetActivityForTodoAsync(todoId);
        return Ok(activity);
    }
}
