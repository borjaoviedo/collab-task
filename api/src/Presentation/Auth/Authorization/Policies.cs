namespace Api.Auth.Authorization
{
    /// <summary>Authorization policy names used across the API.</summary>
    /// <remarks>
    /// Project* policies require membership on the target project route.
    /// SystemAdmin is global and bypasses per-project checks.
    /// </remarks>
    public static class Policies
    {
        /// <summary>Requires at least Owner role in the project.</summary>
        public const string ProjectOwner = "Owner";
        /// <summary>Requires at least Admin role in the project.</summary>
        public const string ProjectAdmin = "Admin";
        /// <summary>Requires at least Member role in the project.</summary>
        public const string ProjectMember = "Member";
        /// <summary>Allows read-only access to project resources.</summary>
        public const string ProjectReader = "Reader";

        /// <summary>Requires platform-wide system administrator privileges.</summary>
        public const string SystemAdmin = "SystemAdmin";
    }
}
