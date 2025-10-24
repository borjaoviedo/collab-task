using Application.TaskNotes.DTOs;
using Application.TaskNotes.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.TaskNotes.Validation
{
    public sealed class TaskNoteDtoValidatorTests
    {
        [Fact]
        public void Create_Valid_Passes()
        {
            var v = new TaskNoteCreateDtoValidator();
            var dto = new TaskNoteCreateDto { Content = "Valid Note Content" };

            var r = v.TestValidate(dto);
            r.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Create_Invalid_Fails()
        {
            var v = new TaskNoteCreateDtoValidator();
            var dto = new TaskNoteCreateDto { Content = "" };

            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(n => n.Content);
        }

        [Fact]
        public void Edit_Valid_Passes()
        {
            var v = new TaskNoteEditDtoValidator();
            var dto = new TaskNoteEditDto { NewContent = "Valid Note Content" };

            var r = v.TestValidate(dto);
            r.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Edit_Invalid_Fails()
        {
            var v = new TaskNoteEditDtoValidator();
            var dto = new TaskNoteEditDto { NewContent = ""};

            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(n => n.NewContent);
        }
    }
}
