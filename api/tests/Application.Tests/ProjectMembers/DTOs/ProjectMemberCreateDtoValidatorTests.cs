using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Validation;
using Application.Tests.Common.Helpers;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.ProjectMembers.DTOs
{
    public sealed class ProjectMemberCreateDtoValidatorTests
    {
        private readonly ProjectMemberCreateDtoValidator _validator = new();

        [Fact]
        public void UserId_Empty_Fails()
        {
            _validator.TestValidate(new ProjectMemberCreateDto
            {
                UserId = Guid.Empty,
                Role = ProjectRole.Member,
                JoinedAt = DateTimeOffset.UtcNow
            })
            .ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void UserId_Valid_Passes()
        {
            _validator.TestValidate(new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Member,
                JoinedAt = DateTimeOffset.UtcNow
            })
            .ShouldNotHaveValidationErrorFor(x => x.UserId);
        }

        [Theory]
        [InlineData(ProjectRole.Owner)]
        [InlineData(ProjectRole.Admin)]
        [InlineData(ProjectRole.Member)]
        [InlineData(ProjectRole.Reader)]
        public void Role_ValidValues_Pass(ProjectRole role)
        {
            _validator.TestValidate(new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = role,
                JoinedAt = DateTimeOffset.UtcNow
            })
            .ShouldNotHaveValidationErrorFor(x => x.Role);
        }

        [Fact]
        public void Role_InvalidValue_Fails()
        {
            _validator.TestValidate(new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = (ProjectRole)999,
                JoinedAt = DateTimeOffset.UtcNow
            })
            .ShouldHaveValidationErrorFor(x => x.Role);
        }

        [Fact]
        public void JoinedAt_InFuture_Fails()
        {
            _validator.TestValidate(new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Member,
                JoinedAt = DateTimeOffset.UtcNow.AddMinutes(5)
            })
            .ShouldHaveValidationErrorFor(x => x.JoinedAt);
        }

        [Fact]
        public void JoinedAt_NotUtc_Fails()
        {
            _validator.TestValidate(new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Member,
                JoinedAt = DateTimes.NonUtcInstant()
            })
            .ShouldHaveValidationErrorFor(x => x.JoinedAt);
        }

        [Fact]
        public void JoinedAt_ExactlyNow_Passes()
        {
            _validator.TestValidate(new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Member,
                JoinedAt = DateTimeOffset.UtcNow
            })
            .ShouldNotHaveValidationErrorFor(x => x.JoinedAt);
        }

        [Fact]
        public void JoinedAt_InPast_Passes()
        {
            _validator.TestValidate(new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Member,
                JoinedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
            })
            .ShouldNotHaveValidationErrorFor(x => x.JoinedAt);
        }

        [Fact]
        public void ValidDto_Passes()
        {
            _validator.TestValidate(new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Admin,
                JoinedAt = DateTimeOffset.UtcNow.AddDays(-1)
            })
            .ShouldNotHaveAnyValidationErrors();
        }
    }
}
