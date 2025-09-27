using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.Application.Services;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
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
        var todoItem = await _todoItemService.GetTodoItemByIdAsync(id);
        return Ok(todoItem);
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

        await _todoItemService.UpdateTodoItemAsync(id, todoItemDto);
        return NoContent();
    }

    // DELETE: api/TodoItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodoItem(int id)
    {
        await _todoItemService.DeleteTodoItemAsync(id);
        return NoContent();
    }

    // Additional actions as needed

    [HttpGet("{id}/tags")]
    public async Task<IActionResult> GetTagsForTodo(int id)
    {
        var tags = await _tagService.GetTagsForTodoAsync(id);
        return Ok(tags);
    }

    [HttpPost("{id}/tags/{tagId}")]
    public async Task<IActionResult> AttachTagToTodo(int id, int tagId)
    {
        await _tagService.AttachTagToTodoAsync(id, tagId);
        return NoContent();
    }

    [HttpDelete("{id}/tags/{tagId}")]
    public async Task<IActionResult> DetachTagFromTodo(int id, int tagId)
    {
        await _tagService.DetachTagFromTodoAsync(id, tagId);
        return NoContent();
    }
}
