using Application.Columns.DTOs;
using Application.Columns.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Columns.Validation
{
    public sealed class ColumnDtoValidatorTests
    {

        [Fact]
        public void ColumnCreate_Invalid_Fails()
        {
            var v = new ColumnCreateDtoValidator();
            var dto = new ColumnCreateDto { ProjectId = Guid.Empty, LaneId = Guid.Empty, Name = "", Order = -1 };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.ProjectId);
            r.ShouldHaveValidationErrorFor(x => x.LaneId);
            r.ShouldHaveValidationErrorFor(x => x.Name);
            r.ShouldHaveValidationErrorFor(x => x.Order);
        }

        [Fact]
        public void Column_Valid_Pass()
        {
            var cv = new ColumnCreateDtoValidator();

            cv.TestValidate(new ColumnCreateDto { ProjectId = Guid.NewGuid(), LaneId = Guid.NewGuid(), Name = "Todo", Order = 0 })
              .ShouldNotHaveAnyValidationErrors();
        }
    }
}
