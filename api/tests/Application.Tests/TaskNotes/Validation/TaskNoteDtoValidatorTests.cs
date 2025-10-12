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
            var dto = new TaskNoteCreateDto { TaskId = Guid.Empty, Content = "" };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.TaskId);
            r.ShouldHaveValidationErrorFor(x => x.Content);
        }

        [Fact]
        public void Edit_Invalid_Fails()
        {
            var v = new TaskNoteEditDtoValidator();
            var dto = new TaskNoteEditDto { Id = Guid.Empty, Content = "", RowVersion = [] };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.Id);
            r.ShouldHaveValidationErrorFor(x => x.Content);
            r.ShouldHaveValidationErrorFor(x => x.RowVersion);
        }

        [Fact]
        public void Delete_Invalid_Fails()
        {
            var v = new TaskNoteDeleteDtoValidator();
            var dto = new TaskNoteDeleteDto { Id = Guid.Empty, RowVersion = [] };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.Id);
            r.ShouldHaveValidationErrorFor(x => x.RowVersion);
        }
    }
}
