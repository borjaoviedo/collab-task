using Application.TaskNotes.DTOs;
using Application.TaskNotes.Validation;
using FluentValidation.TestHelper;
using TestHelpers.Common.Testing;

namespace Application.Tests.TaskNotes.Validation
{
    [UnitTest]
    public sealed class TaskNoteDtoValidatorTests
    {
        [Fact]
        public void Create_Valid_Passes()
        {
            var validator = new TaskNoteCreateDtoValidator();
            var dto = new TaskNoteCreateDto { Content = "Valid Note Content" };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Create_Invalid_Fails()
        {
            var validator = new TaskNoteCreateDtoValidator();
            var dto = new TaskNoteCreateDto { Content = "" };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldHaveValidationErrorFor(n => n.Content);
        }

        [Fact]
        public void Edit_Valid_Passes()
        {
            var validator = new TaskNoteEditDtoValidator();
            var dto = new TaskNoteEditDto { NewContent = "Valid Note Content" };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Edit_Invalid_Fails()
        {
            var validator = new TaskNoteEditDtoValidator();
            var dto = new TaskNoteEditDto { NewContent = ""};

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldHaveValidationErrorFor(n => n.NewContent);
        }
    }
}
