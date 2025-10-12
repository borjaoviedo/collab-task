using Application.Common.Validation.Extensions;
using Application.TaskNotes.DTOs;
using FluentValidation;

namespace Application.TaskNotes.Validation
{
    public sealed class TaskNoteCreateDtoValidator : AbstractValidator<TaskNoteCreateDto>
    {
        public TaskNoteCreateDtoValidator()
        {
            RuleFor(n => n.Content).NoteContentRules();
        }
    }
}
