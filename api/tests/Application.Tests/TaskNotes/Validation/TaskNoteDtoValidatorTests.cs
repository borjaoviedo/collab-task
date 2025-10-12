using Application.TaskNotes.DTOs;
using Application.TaskNotes.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.TaskNotes.Validation
{
    public sealed class TaskNoteDtoValidatorTests
    {
        [Fact]
        public void Create_Invalid_Fails()
        {
            var v = new TaskNoteCreateDtoValidator();
            var dto = new TaskNoteCreateDto { Content = "" };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.Content);
        }

        [Fact]
        public void Edit_Invalid_Fails()
        {
            var v = new TaskNoteEditDtoValidator();
            var dto = new TaskNoteEditDto { Content = ""};
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.Content);
        }
    }
}
