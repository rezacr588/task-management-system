using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.Interfaces;
using TodoApi.Application.DTOs;
using TodoApi.Application.Services;

[Route("api/[controller]")]
[ApiController]
public class TodoItemsController : ControllerBase
{
    private readonly ITodoItemService _todoItemService;

    public TodoItemsController(ITodoItemService todoItemService)
    {
        _todoItemService = todoItemService;
    }

    // GET: api/TodoItems
    [HttpGet]
    public async Task<IActionResult> GetAllTodoItems()
    {
        var todoItems = await _todoItemService.GetAllTodoItemsAsync();
        return Ok(todoItems);
    }

    // GET: api/TodoItems/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTodoItem(int id)
    {
        var todoItem = await _todoItemService.GetTodoItemByIdAsync(id);

        if (todoItem == null)
        {
            return NotFound();
        }

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
}
