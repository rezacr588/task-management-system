using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class TodoItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; }

    [StringLength(500)]
    public string Description { get; set; }

    public bool IsComplete { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime DueDate { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? CompletedDate { get; set; }

    public PriorityLevel Priority { get; set; }

    public int? AssignedToUserId { get; set; }
    public virtual User AssignedToUser { get; set; }

    public virtual ICollection<Tag> Tags { get; set; } = new HashSet<Tag>();
}