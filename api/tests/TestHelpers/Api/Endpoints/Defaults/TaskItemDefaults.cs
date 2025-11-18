using Application.TaskItems.DTOs;

namespace TestHelpers.Api.Endpoints.Defaults
{
    public static class TaskItemDefaults
    {
        public readonly static string DefaultTaskTitle = "title";
        public readonly static string DefaultTaskDescription = "description";
        public readonly static DateTimeOffset? DefaultDueDate = null;
        public readonly static decimal DefaultSortKey = 0m;

        public readonly static TaskItemCreateDto DefaultTaskCreateDto = new()
        {
            Title = DefaultTaskTitle,
            Description = DefaultTaskDescription,
            DueDate = DefaultDueDate,
            SortKey = DefaultSortKey
        };
    }
}
