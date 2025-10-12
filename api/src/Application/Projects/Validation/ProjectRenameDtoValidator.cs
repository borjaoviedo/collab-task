using Application.Common.Validation.Extensions;
using Application.Projects.DTOs;
using FluentValidation;

namespace Application.Projects.Validation
{
    public sealed class ProjectRenameDtoValidator : AbstractValidator<ProjectRenameDto>
    {
        public ProjectRenameDtoValidator()
        {
            RuleFor(x => x.NewName).ProjectNameRules();
        }
    }
}
