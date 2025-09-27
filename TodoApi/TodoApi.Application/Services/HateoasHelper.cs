using TodoApi.Application.DTOs;

namespace TodoApi.Application.Services
{
    public static class HateoasHelper
    {
        public static void AddTodoItemLinks(TodoItemDto todoItem, string baseUrl = "")
        {
            todoItem.Links.Add(new Link
            {
                Rel = "self",
                Href = $"{baseUrl}/api/v1/TodoItems/{todoItem.Id}",
                Method = "GET"
            });

            todoItem.Links.Add(new Link
            {
                Rel = "update",
                Href = $"{baseUrl}/api/v1/TodoItems/{todoItem.Id}",
                Method = "PUT",
                Type = "application/json"
            });

            todoItem.Links.Add(new Link
            {
                Rel = "delete",
                Href = $"{baseUrl}/api/v1/TodoItems/{todoItem.Id}",
                Method = "DELETE"
            });

            if (!todoItem.IsComplete)
            {
                todoItem.Links.Add(new Link
                {
                    Rel = "complete",
                    Href = $"{baseUrl}/api/v1/TodoItems/{todoItem.Id}/complete",
                    Method = "PATCH",
                    Type = "application/json"
                });
            }

            todoItem.Links.Add(new Link
            {
                Rel = "comments",
                Href = $"{baseUrl}/api/v1/Comments/todo/{todoItem.Id}",
                Method = "GET"
            });

            todoItem.Links.Add(new Link
            {
                Rel = "activity",
                Href = $"{baseUrl}/api/v1/Comments/todo/{todoItem.Id}/activity",
                Method = "GET"
            });
        }

        public static void AddTodoItemsCollectionLinks(List<TodoItemDto> todoItems, int pageNumber, int pageSize, int totalCount, string baseUrl = "")
        {
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Add self link for current page
            var selfLink = new Link
            {
                Rel = "self",
                Href = $"{baseUrl}/api/v1/TodoItems?pageNumber={pageNumber}&pageSize={pageSize}",
                Method = "GET"
            };

            // Add pagination links
            if (pageNumber > 1)
            {
                todoItems.Insert(0, new TodoItemDto()); // Placeholder for first link
                todoItems[0].Links.Add(new Link
                {
                    Rel = "first",
                    Href = $"{baseUrl}/api/v1/TodoItems?pageNumber=1&pageSize={pageSize}",
                    Method = "GET"
                });

                todoItems[0].Links.Add(new Link
                {
                    Rel = "previous",
                    Href = $"{baseUrl}/api/v1/TodoItems?pageNumber={pageNumber - 1}&pageSize={pageSize}",
                    Method = "GET"
                });
            }

            if (pageNumber < totalPages)
            {
                if (todoItems.Count == 0) todoItems.Add(new TodoItemDto()); // Ensure we have a place for links
                todoItems[0].Links.Add(new Link
                {
                    Rel = "next",
                    Href = $"{baseUrl}/api/v1/TodoItems?pageNumber={pageNumber + 1}&pageSize={pageSize}",
                    Method = "GET"
                });

                todoItems[0].Links.Add(new Link
                {
                    Rel = "last",
                    Href = $"{baseUrl}/api/v1/TodoItems?pageNumber={totalPages}&pageSize={pageSize}",
                    Method = "GET"
                });
            }

            // Add create link
            if (todoItems.Count == 0) todoItems.Add(new TodoItemDto());
            todoItems[0].Links.Add(new Link
            {
                Rel = "create",
                Href = $"{baseUrl}/api/v1/TodoItems",
                Method = "POST",
                Type = "application/json"
            });
        }
    }
}