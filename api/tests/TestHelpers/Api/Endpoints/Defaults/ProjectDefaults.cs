using Application.Projects.DTOs;

namespace TestHelpers.Api.Endpoints.Defaults
{
    public static class ProjectDefaults
    {
        public readonly static string DefaultProjectName = "project";
        public readonly static string DefaultProjectRename = "different name";

        public readonly static ProjectCreateDto DefaultProjectCreateDto = new() { Name = DefaultProjectName };
        public readonly static ProjectRenameDto DefaultProjectRenameDto = new() { NewName = DefaultProjectRename };
    }
}
