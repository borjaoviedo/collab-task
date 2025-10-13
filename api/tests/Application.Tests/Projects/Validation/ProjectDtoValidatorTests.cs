using Application.Common.Validation.Extensions;
using FluentValidation;
using FluentValidation.TestHelper;

namespace Application.Tests.Projects.Validation
{
    public sealed class ProjectDtoValidatorTests
    {
        private sealed class ProjectCreateDto
        {
            public string Name { get; set; } = "";
        }

        private sealed class ProjectCreateDtoValidator : AbstractValidator<ProjectCreateDto>
        {
            public ProjectCreateDtoValidator()
            {
                RuleFor(p => p.Name).ProjectNameRules();
            }
        }

        [Fact]
        public void ProjectName_Invalid_Fails()
        {
            var v = new ProjectCreateDtoValidator();
            v.TestValidate(new ProjectCreateDto { Name = "  " })
             .ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void ProjectName_Valid_Passes()
        {
            var v = new ProjectCreateDtoValidator();
            v.TestValidate(new ProjectCreateDto { Name = "My Project" })
             .ShouldNotHaveValidationErrorFor(x => x.Name);
        }
    }
}
