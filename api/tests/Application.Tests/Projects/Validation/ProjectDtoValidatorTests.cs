using Application.Projects.DTOs;
using Application.Projects.Validation;
using FluentValidation.TestHelper;
using TestHelpers.Common.Testing;

namespace Application.Tests.Projects.Validation
{
    [UnitTest]
    public sealed class ProjectDtoValidatorTests
    {
        [Fact]
        public void Project_Valid_Passes()
        {
            var validator = new ProjectCreateDtoValidator();
            var dto = new ProjectCreateDto { Name = "My Project" };

            var validationResult = validator.TestValidate(dto);
            validationResult.ShouldNotHaveValidationErrorFor(p => p.Name);
        }

        [Fact]
        public void Project_Invalid_Fails()
        {
            var validator = new ProjectCreateDtoValidator();

            validator.TestValidate(new ProjectCreateDto { Name = "" })
             .ShouldHaveValidationErrorFor(p => p.Name)
             .WithErrorMessage("Project name cannot be whitespace.");

            validator.TestValidate(new ProjectCreateDto { Name = "in  valid" })
             .ShouldHaveValidationErrorFor(p => p.Name)
             .WithErrorMessage("Project name cannot contain consecutive spaces.");

            validator.TestValidate(new ProjectCreateDto { Name = new string('x', 101) })
             .ShouldHaveValidationErrorFor(p => p.Name)
             .WithErrorMessage("Project name length must be at most 100 characters.");
        }
    }
}
