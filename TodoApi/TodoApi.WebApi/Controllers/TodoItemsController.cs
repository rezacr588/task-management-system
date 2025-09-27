using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.Application.Services;

[Route("api/[controller]")]
[ApiController]
public class TodoItemsController : ControllerBase
{
    private readonly ITodoItemService _todoItemService;
    private readonly ITagService _tagService;

    public TodoItemsController(ITodoItemService todoItemService, ITagService tagService)
    {
        _todoItemService = todoItemService;
        _tagService = tagService;
    }

    // GET: api/TodoItems
    [HttpGet]
    public async Task<IActionResult> GetAllTodoItems()
    {

        var todoItems = await _todoItemService.GetAllTodoItemsAsync(
            filter: null
        );

        return Ok(todoItems);
    }

    // GET: api/TodoItems/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTodoItem(int id)
    {
        try
        {
            var todoItem = await _todoItemService.GetTodoItemByIdAsync(id);
            return Ok(todoItem);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // POST: api/TodoItems
    [HttpPost]
    public async Task<IActionResult> CreateTodoItem([FromBody] TodoItemDto todoItemDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdTodoItem = await _todoItemService.CreateTodoItemAsync(todoItemDto);
        return CreatedAtAction(nameof(GetTodoItem), new { id = createdTodoItem.Id }, createdTodoItem);
    }

    // PUT: api/TodoItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodoItem(int id, [FromBody] TodoItemDto todoItemDto)
    {
        if (id != todoItemDto.Id)
        {
            return BadRequest();
        }

        try
        {
            await _todoItemService.UpdateTodoItemAsync(id, todoItemDto);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // DELETE: api/TodoItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodoItem(int id)
    {
        try
        {
            await _todoItemService.DeleteTodoItemAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // Additional actions as needed

    [HttpGet("{id}/tags")]
    public async Task<IActionResult> GetTagsForTodo(int id)
    {
        try
        {
            var tags = await _tagService.GetTagsForTodoAsync(id);
            return Ok(tags);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/tags/{tagId}")]
    public async Task<IActionResult> AttachTagToTodo(int id, int tagId)
    {
        try
        {
            await _tagService.AttachTagToTodoAsync(id, tagId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}/tags/{tagId}")]
    public async Task<IActionResult> DetachTagFromTodo(int id, int tagId)
    {
        try
        {
            await _tagService.DetachTagFromTodoAsync(id, tagId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
