using Domain.Enums;

namespace Application.Projects.Filters
{
    public sealed class ProjectFilter
    {
        public string? NameContains { get; init; }
        public ProjectRole? Role { get; init; }
        public bool? IncludeRemoved { get; init; }
        public int? Skip { get; init; }
        public int? Take { get; init; }
        public ProjectOrderBy? OrderBy { get; init; }
    }
}
