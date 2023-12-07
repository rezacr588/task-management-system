using TodoApi;
using Microsoft.EntityFrameworkCore;

public class TodoItemService
{
    private readonly ApplicationDbContext _context;

    public TodoItemService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TodoItem> CreateTodoItemAsync(TodoItem todoItem)
    {
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();
        return todoItem;
    }

    public async Task<TodoItem> GetTodoItemByIdAsync(int id)
    {
        return await _context.TodoItems.FindAsync(id);
    }

    public async Task<IEnumerable<TodoItem>> GetAllTodoItemsAsync(string filter)
    {
        // Start with a queryable that represents all TodoItems in the database
        var query = _context.TodoItems.AsQueryable();

        // Check if a filter string is provided
        if (!string.IsNullOrEmpty(filter))
        {
            // If there is a filter, apply it to the query.
            // This example assumes the filter is applied to the Title property of TodoItem.
            // Adjust this logic based on your specific filtering requirements.
            // For instance, you could filter by other properties, or implement more complex filtering logic.
            query = query.Where(t => t.Title.Contains(filter));
        }

        // Execute the query asynchronously and return the result as a list.
        // ToListAsync() is an Entity Framework Core method that executes the query against the database
        // and converts the result to a List<TodoItem>, which is then returned from the method.
        return await query.ToListAsync();
    }

    public async Task<TodoItem> UpdateTodoItemAsync(int id, TodoItem updatedTodoItem)
    {
        // Find the existing TodoItem in the database by its id.
        // The FindAsync method is used to retrieve an entity by its primary key.
        var todoItem = await _context.TodoItems.FindAsync(id);

        // If no TodoItem with the specified id is found, return null.
        if (todoItem == null)
        {
            return null;
        }

        // If the TodoItem is found, update its properties with the values from updatedTodoItem.
        // This is where you map the updated values to the existing entity.
        // Below are examples of such mappings. You should update these lines to reflect the properties of TodoItem.
        todoItem.Title = updatedTodoItem.Title;
        todoItem.Description = updatedTodoItem.Description;
        todoItem.IsComplete = updatedTodoItem.IsComplete;
        todoItem.DueDate = updatedTodoItem.DueDate;
        todoItem.Priority = updatedTodoItem.Priority;
        // Add other properties that need to be updated.

        // Save the changes to the database.
        // The SaveChangesAsync method applies the changes made to the DbContext to the database.
        await _context.SaveChangesAsync();

        // Return the updated TodoItem.
        return todoItem;
    }

    public async Task<bool> DeleteTodoItemAsync(int id)
    {
        var todoItem = await _context.TodoItems.FindAsync(id);
        if (todoItem == null)
        {
            return false;
        }

        _context.TodoItems.Remove(todoItem);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TodoItem> MarkTodoItemCompleteAsync(int id, bool isComplete)
    {
        var todoItem = await _context.TodoItems.FindAsync(id);
        if (todoItem == null)
        {
            return null;
        }

        todoItem.IsComplete = isComplete;
        if (isComplete)
        {
            todoItem.CompletedDate = DateTime.UtcNow;
        }
        else
        {
            todoItem.CompletedDate = null;
        }

        await _context.SaveChangesAsync();
        return todoItem;
    }
}
