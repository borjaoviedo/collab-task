using Application.Common.Validation.Extensions;
using Application.TaskNotes.DTOs;
using FluentValidation;

namespace Application.TaskNotes.Validation
{
    public sealed class TaskNoteDeleteDtoValidator : AbstractValidator<TaskNoteDeleteDto>
    {
        public TaskNoteDeleteDtoValidator()
        {
            RuleFor(n => n.Id).RequiredGuid();
            RuleFor(n => n.RowVersion).ConcurrencyTokenRules();
        }
    }
}
