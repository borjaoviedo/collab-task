using Application.Projects.DTOs;
using Application.Projects.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Projects.DTOs
{
    public sealed class ProjectUpdateDtoValidatorTests
    {
        private readonly ProjectUpdateDtoValidator _validator = new();

        [Fact]
        public void Name_Empty_Fails()
            => _validator.TestValidate(new ProjectUpdateDto { Name = "", RowVersion = [1] })
                .ShouldHaveValidationErrorFor(x => x.Name);

        [Fact]
        public void RowVersion_Empty_Fails()
            => _validator.TestValidate(new ProjectUpdateDto { Name = "Valid Name", RowVersion = [] })
                .ShouldHaveValidationErrorFor(x => x.RowVersion);

        [Theory]
        [InlineData(" ")]
        [InlineData("Two Consecutive  Spaces")]
        [InlineData("Invalid Character \0")]
        public void Name_Invalid_Fails(string input)
            => _validator.TestValidate(new ProjectUpdateDto { Name = input, RowVersion = [1] })
                .ShouldHaveValidationErrorFor(x => x.Name);

        [Fact]
        public void Name_TooLong_Fails()
        {
            _validator.TestValidate(new ProjectUpdateDto { Name = new string('x', 101), RowVersion = [1] })
                .ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void ValidDto_Passes()
            => _validator.TestValidate(new ProjectUpdateDto { Name = "Valid DTO", RowVersion = [1] })
                .ShouldNotHaveAnyValidationErrors();

        [Fact]
        public void Null_Name_Passes()
            => _validator.TestValidate(new ProjectUpdateDto { Name = null, RowVersion = [1] })
                .ShouldNotHaveAnyValidationErrors();

        [Fact]
        public void Null_Name_And_Empty_RowVersion_Fails()
            => _validator.TestValidate(new ProjectUpdateDto { Name = null, RowVersion = [] })
                .ShouldHaveValidationErrorFor(x => x.RowVersion);
    }
}
