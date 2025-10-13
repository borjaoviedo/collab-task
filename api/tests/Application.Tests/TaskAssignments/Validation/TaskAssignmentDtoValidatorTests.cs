using Application.TaskAssignments.DTOs;
using Application.TaskAssignments.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.TaskAssignments.Validation
{
    public sealed class TaskAssignmentDtoValidatorTests
    {
        [Fact]
        public void Create_Invalid_Fails()
        {
            var v = new TaskAssignmentCreateDtoValidator();
            var dto = new TaskAssignmentCreateDto { UserId = Guid.Empty, Role = (TaskRole)999 };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.UserId);
            r.ShouldHaveValidationErrorFor(x => x.Role);
        }
    }
}
