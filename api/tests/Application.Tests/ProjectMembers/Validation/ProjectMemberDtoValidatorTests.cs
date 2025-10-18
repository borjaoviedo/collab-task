using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Validation;
using Application.Tests.Common.Helpers;
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
                JoinedAt = DateTimes.NonUtcInstant()
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
        public void ChangeRole_Invalid_Fails()
        {
            var v = new ProjectMemberChangeRoleDtoValidator();
            var dto = new ProjectMemberChangeRoleDto
            {
                NewRole = (ProjectRole)999
            };
            var r = v.TestValidate(dto);
            r.ShouldHaveValidationErrorFor(x => x.NewRole).WithErrorMessage("Invalid project role value.");
        }

        [Fact]
        public void ChangeRole_Valid_Passes()
        {
            var v = new ProjectMemberChangeRoleDtoValidator();
            var dto = new ProjectMemberChangeRoleDto
            {
                NewRole = ProjectRole.Member
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
                RemovedAt = DateTimes.NonUtcInstant(), // not UTC
            });
            r1.ShouldHaveValidationErrorFor(x => x.RemovedAt).WithErrorMessage("RemovedAt must be in UTC.");

            // Future
            var r2 = v.TestValidate(new ProjectMemberRemoveDto
            {
                RemovedAt = DateTimeOffset.UtcNow.AddMinutes(1)
            });
            r2.ShouldHaveValidationErrorFor(x => x.RemovedAt).WithErrorMessage("RemovedAt cannot be in the future.");
        }

        [Fact]
        public void Remove_Valid_Passes()
        {
            var v = new ProjectMemberRemoveDtoValidator();
            var dto = new ProjectMemberRemoveDto
            {
                RemovedAt = DateTimeOffset.UtcNow
            };
            v.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
        }
    }
}
