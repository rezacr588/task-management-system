using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    public virtual ICollection<TodoItem> AssignedTodoItems { get; set; } = new HashSet<TodoItem>();
}