using Application.Lanes.DTOs;
using Application.Lanes.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Lanes.Validation
{
    public sealed class LaneDtoValidatorTests
    {
        [Fact]
        public void LaneCreate_Invalid_Fails()
        {
            var v = new LaneCreateDtoValidator();
            var dto = new LaneCreateDto { Name = "  ", Order = -1 };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.Name);
            r.ShouldHaveValidationErrorFor(x => x.Order);
        }

        [Fact]
        public void Lane_Valid_Pass()
        {
            var lv = new LaneCreateDtoValidator();

            lv.TestValidate(new LaneCreateDto { Name = "Backlog", Order = 0 })
              .ShouldNotHaveAnyValidationErrors();
        }
    }
}
