using Application.TaskActivities.DTOs;
using Application.TaskActivities.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.TaskActivities.Validation
{
    public sealed class TaskActivityDtoValidatorTests
    {
        [Fact]
        public void Create_Invalid_Fails()
        {
            var v = new TaskActivityCreateDtoValidator();
            var dto = new TaskActivityCreateDto { Type = (TaskActivityType)999, Payload = "" };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.Type);
            r.ShouldHaveValidationErrorFor(x => x.Payload);
        }

        [Fact]
        public void Create_Payload_Must_Be_ValidJson()
        {
            var v = new TaskActivityCreateDtoValidator();
            v.TestValidate(new TaskActivityCreateDto
            {
                Type = TaskActivityType.TaskCreated,
                Payload = "{not json}"
            }).ShouldHaveValidationErrorFor(x => x.Payload);

            v.TestValidate(new TaskActivityCreateDto
            {
                Type = TaskActivityType.TaskEdited,
                Payload = "{\"k\":1}"
            }).ShouldNotHaveValidationErrorFor(x => x.Payload);
        }
    }
}
