using Domain.Enums;

namespace Application.Projects.Abstractions
{
    public sealed class ProjectFilter
    {
        public string? NameContains { get; set; }
        public ProjectRole? Role { get; set; }
        public bool? IncludeRemoved { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
        public string? OrderBy { get; set; }
    }
}
