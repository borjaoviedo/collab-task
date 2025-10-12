using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.ProjectMembers.Validation
{
    public sealed class ProjectMemberDtoValidatorTests
    {
        [Fact]
        public void Create_Invalid_Fails()
        {
            var v = new ProjectMemberCreateDtoValidator();
            var dto = new ProjectMemberCreateDto
            {
                UserId = Guid.Empty,
                Role = (ProjectRole)999,
                JoinedAt = new DateTimeOffset(1999, 12, 31, 23, 59, 59, TimeSpan.Zero) // too old
            };

            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.UserId).WithErrorMessage("Id is required.");
            r.ShouldHaveValidationErrorFor(x => x.Role).WithErrorMessage("Invalid project role value.");
            r.ShouldHaveValidationErrorFor(x => x.JoinedAt).WithErrorMessage("JoinedAt is too old.");
        }

        [Fact]
        public void Create_NotUtcOrFuture_Fails()
        {
            var v = new ProjectMemberCreateDtoValidator();

            // Not UTC
            var r1 = v.TestValidate(new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Member,
                JoinedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.FromHours(+2))
            });
            r1.ShouldHaveValidationErrorFor(x => x.JoinedAt).WithErrorMessage("JoinedAt must be in UTC.");

            // Future
            var r2 = v.TestValidate(new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Member,
                JoinedAt = DateTimeOffset.UtcNow.AddMinutes(5)
            });
            r2.ShouldHaveValidationErrorFor(x => x.JoinedAt).WithErrorMessage("JoinedAt cannot be in the future.");
        }

        [Fact]
        public void Create_Valid_Passes()
        {
            var v = new ProjectMemberCreateDtoValidator();
            var dto = new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Admin,
                JoinedAt = DateTimeOffset.UtcNow.AddDays(-1)
            };
            v.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void UpdateRole_Invalid_Fails()
        {
            var v = new ProjectMemberUpdateRoleDtoValidator();
            var dto = new ProjectMemberUpdateRoleDto
            {
                Role = (ProjectRole)999,
                RowVersion = []
            };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.Role).WithErrorMessage("Invalid project role value.");
            r.ShouldHaveValidationErrorFor(x => x.RowVersion).WithErrorMessage("RowVersion cannot be empty.");
        }

        [Fact]
        public void UpdateRole_Valid_Passes()
        {
            var v = new ProjectMemberUpdateRoleDtoValidator();
            var dto = new ProjectMemberUpdateRoleDto
            {
                Role = ProjectRole.Member,
                RowVersion = [1]
            };
            v.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Remove_Invalid_Fails()
        {
            var v = new ProjectMemberRemoveDtoValidator();

            // Not UTC
            var r1 = v.TestValidate(new ProjectMemberRemoveDto
            {
                RemovedAt = DateTimeOffset.Now, // not UTC
                RowVersion = []
            });
            r1.ShouldHaveValidationErrorFor(x => x.RemovedAt).WithErrorMessage("RemovedAt must be in UTC.");
            r1.ShouldHaveValidationErrorFor(x => x.RowVersion).WithErrorMessage("RowVersion cannot be empty.");

            // Future
            var r2 = v.TestValidate(new ProjectMemberRemoveDto
            {
                RemovedAt = DateTimeOffset.UtcNow.AddMinutes(1),
                RowVersion = [1]
            });
            r2.ShouldHaveValidationErrorFor(x => x.RemovedAt).WithErrorMessage("RemovedAt cannot be in the future.");
        }

        [Fact]
        public void Remove_Valid_Passes()
        {
            var v = new ProjectMemberRemoveDtoValidator();
            var dto = new ProjectMemberRemoveDto
            {
                RemovedAt = DateTimeOffset.UtcNow,
                RowVersion = [1]
            };
            v.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
        }
    }
}
