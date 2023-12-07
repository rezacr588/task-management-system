using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/todos")]
public class TodoItemController : ControllerBase
{
    private readonly TodoItemService _todoItemService;

    public TodoItemController(TodoItemService todoItemService)
    {
        _todoItemService = todoItemService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTodoItem(TodoItem todoItem)
    {
        var createdItem = await _todoItemService.CreateTodoItemAsync(todoItem);
        return CreatedAtAction(nameof(GetTodoItemById), new { id = createdItem.Id }, createdItem);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTodoItemById(int id)
    {
        var item = await _todoItemService.GetTodoItemByIdAsync(id);
        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTodoItems(string filter = null)
    {
        var items = await _todoItemService.GetAllTodoItemsAsync(filter);
        return Ok(items);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodoItem(int id, TodoItem todoItem)
    {
        var updatedItem = await _todoItemService.UpdateTodoItemAsync(id, todoItem);
        if (updatedItem == null)
            return NotFound();

        return Ok(updatedItem);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodoItem(int id)
    {
        var result = await _todoItemService.DeleteTodoItemAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> MarkTodoItemComplete(int id, bool isComplete)
    {
        var updatedItem = await _todoItemService.MarkTodoItemCompleteAsync(id, isComplete);
        if (updatedItem == null)
            return NotFound();

        return Ok(updatedItem);
    }
}
