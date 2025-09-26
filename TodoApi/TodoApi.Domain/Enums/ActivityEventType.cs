namespace TodoApi.Domain.Enums
{
    public enum ActivityEventType
    {
        CommentCreated = 1,
        CommentUpdated = 2,
        CommentDeleted = 3,
        StatusChanged = 4,
        PriorityChanged = 5,
        AssignmentChanged = 6,
        DueDateChanged = 7,
        TaskCreated = 8,
        TaskUpdated = 9,
        TaskCompleted = 10,
        TaskReopened = 11
    }
}
