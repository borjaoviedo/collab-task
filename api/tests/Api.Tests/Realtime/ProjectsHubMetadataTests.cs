using Api.Realtime;

namespace Api.Tests.Realtime
{
    public sealed class ProjectsHubMetadataTests
    {
        [Fact]
        public void ProjectsHub_Has_Authorize_Attribute()
        {
            var hubType = typeof(ProjectsHub);
            var hasAuthorize = hubType
                .GetCustomAttributes(inherit: true)
                .Any(a => a.GetType().Name == "AuthorizeAttribute");

            Assert.True(hasAuthorize);
        }

        [Fact]
        public void Join_And_Leave_Project_Take_Guid_Parameter()
        {
            var hubType = typeof(ProjectsHub);
            var join = hubType.GetMethod("JoinProject");
            var leave = hubType.GetMethod("LeaveProject");

            Assert.NotNull(join);
            Assert.NotNull(leave);
            Assert.Single(join!.GetParameters());
            Assert.Single(leave!.GetParameters());
            Assert.Equal(typeof(Guid), join.GetParameters()[0].ParameterType);
            Assert.Equal(typeof(Guid), leave.GetParameters()[0].ParameterType);
        }
    }
}
