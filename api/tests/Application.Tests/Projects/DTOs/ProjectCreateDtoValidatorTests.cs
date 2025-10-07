using Application.Projects.DTOs;
using Application.Projects.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Projects.DTOs
{
    public sealed class ProjectCreateDtoValidatorTests
    {
        private readonly ProjectCreateDtoValidator _validator = new();

        [Fact]
        public void Name_Empty_Fails()
            => _validator.TestValidate(new ProjectCreateDto { Name = "" })
                .ShouldHaveValidationErrorFor(x => x.Name);

        [Theory]
        [InlineData(" ")]
        [InlineData("Two Consecutive  Spaces")]
        [InlineData("Invalid Character \0")]
        public void Name_Invalid_Fails(string input)
            => _validator.TestValidate(new ProjectCreateDto { Name = input })
                .ShouldHaveValidationErrorFor(x => x.Name);

        [Fact]
        public void Name_TooLong_Fails()
        {
            _validator.TestValidate(new ProjectCreateDto { Name = new string('x', 101) })
                .ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void ValidDto_Passes()
            => _validator.TestValidate(new ProjectCreateDto {Name = "Valid DTO"})
                .ShouldNotHaveAnyValidationErrors();
    }
}
