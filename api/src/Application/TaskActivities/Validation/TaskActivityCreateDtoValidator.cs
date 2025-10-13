using Application.Common.Validation.Extensions;
using Application.TaskActivities.DTOs;
using FluentValidation;

namespace Application.TaskActivities.Validation
{
    public sealed class TaskActivityCreateDtoValidator : AbstractValidator<TaskActivityCreateDto>
    {
        public TaskActivityCreateDtoValidator()
        {
            RuleFor(a => a.Type).TaskActivityTypeRules();
            RuleFor(a => a.Payload).ActivityPayloadRules();
        }
    }
}
