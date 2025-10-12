using Application.Common.Validation.Extensions;
using Application.TaskNotes.DTOs;
using FluentValidation;

namespace Application.TaskNotes.Validation
{
    public sealed class TaskNoteEditDtoValidator : AbstractValidator<TaskNoteEditDto>
    {
        public TaskNoteEditDtoValidator()
        {
            RuleFor(n => n.Content).NoteContentRules();
        }
    }
}
