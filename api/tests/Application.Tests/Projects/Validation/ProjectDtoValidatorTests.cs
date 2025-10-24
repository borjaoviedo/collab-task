using Application.Projects.DTOs;
using Application.Projects.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Projects.Validation
{
    public sealed class ProjectDtoValidatorTests
    {
        [Fact]
        public void Project_Valid_Passes()
        {
            var v = new ProjectCreateDtoValidator();
            var dto = new ProjectCreateDto { Name = "My Project" };

            var r = v.TestValidate(dto);
            r.ShouldNotHaveValidationErrorFor(p => p.Name);
        }

        [Fact]
        public void Project_Invalid_Fails()
        {
            var v = new ProjectCreateDtoValidator();

            v.TestValidate(new ProjectCreateDto { Name = "" })
             .ShouldHaveValidationErrorFor(p => p.Name)
             .WithErrorMessage("Project name cannot be whitespace.");

            v.TestValidate(new ProjectCreateDto { Name = "in  valid" })
             .ShouldHaveValidationErrorFor(p => p.Name)
             .WithErrorMessage("Project name cannot contain consecutive spaces.");

            v.TestValidate(new ProjectCreateDto { Name = new string('x', 101) })
             .ShouldHaveValidationErrorFor(p => p.Name)
             .WithErrorMessage("Project name length must be at most 100 characters.");
        }
    }
}
