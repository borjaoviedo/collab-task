using Application.TaskNotes.DTOs;

namespace TestHelpers.Api.Endpoints.Defaults
{
    public static class TaskNoteDefaults
    {
        public readonly static string DefaultNoteContent = "note content";
        public readonly static string DefaultNoteNewContent = "new content";

        public readonly static TaskNoteCreateDto DefaultNoteCreateDto = new()
        {
            Content = DefaultNoteContent
        };
        public readonly static TaskNoteEditDto DefaultNoteEditDto = new()
        {
            NewContent = DefaultNoteNewContent
        };
    }
}
