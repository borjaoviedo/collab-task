using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.ProjectMembers.DTOs
{
    public sealed class ProjectMemberRemoveDtoValidatorTests
    {
        private readonly ProjectMemberRemoveDtoValidator _validator = new();

        [Fact]
        public void RemovedAt_InFuture_Fails()
        {
            _validator.TestValidate(new ProjectMemberRemoveDto { RemovedAt = DateTimeOffset.UtcNow.AddMinutes(5), RowVersion = [1] })
                .ShouldHaveValidationErrorFor(x => x.RemovedAt);
        }

        [Fact]
        public void RemovedAt_NotUtc_Fails()
        {
            _validator.TestValidate(new ProjectMemberRemoveDto { RemovedAt = DateTimeOffset.Now, RowVersion = [1] })
                .ShouldHaveValidationErrorFor(x => x.RemovedAt);
        }

        [Fact]
        public void RemovedAt_Null_Passes()
        {
            _validator.TestValidate(new ProjectMemberRemoveDto { RemovedAt = null, RowVersion = [1] })
                .ShouldNotHaveValidationErrorFor(x => x.RemovedAt);
        }

        [Fact]
        public void RemovedAt_ExactlyNow_Passes()
        {
            _validator.TestValidate(new ProjectMemberRemoveDto { RemovedAt = DateTimeOffset.UtcNow, RowVersion = [1] })
                .ShouldNotHaveValidationErrorFor(x => x.RemovedAt);
        }

        [Fact]
        public void RemovedAt_InPast_Passes()
        {
            _validator.TestValidate(new ProjectMemberRemoveDto { RemovedAt = DateTimeOffset.UtcNow.AddMinutes(-5), RowVersion = [1] })
                .ShouldNotHaveValidationErrorFor(x => x.RemovedAt);
        }

        [Fact]
        public void RowVersion_Empty_Fails()
        {
            _validator.TestValidate(new ProjectMemberRemoveDto { RemovedAt = DateTimeOffset.UtcNow.AddDays(-1), RowVersion = [] })
                .ShouldHaveValidationErrorFor(x => x.RowVersion);
        }

        [Fact]
        public void Valid_RemovedAt_And_RowVersion_Passes()
        {
            _validator.TestValidate(new ProjectMemberRemoveDto { RemovedAt = DateTimeOffset.UtcNow.AddDays(-3), RowVersion = [10] })
                .ShouldNotHaveAnyValidationErrors();
        }
    }
}
