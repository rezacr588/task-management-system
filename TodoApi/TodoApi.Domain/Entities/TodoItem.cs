using System.ComponentModel.DataAnnotations;
using TodoApi.Domain.Enums;

namespace TodoApi.Domain.Entities
{
    public class TodoItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Title { get; set; }

        [StringLength(500)]
        public required string Description { get; set; }

        public bool IsComplete { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime DueDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? CompletedDate { get; set; }

        public PriorityLevel Priority { get; set; }

        public int? AssignedToUserId { get; set; }
        public virtual required User AssignedToUser { get; set; }

        public virtual ICollection<Tag> Tags { get; set; } = new HashSet<Tag>();
        
    }
}
