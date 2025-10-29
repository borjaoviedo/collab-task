namespace Api.Auth.Authorization
{
    public static class Policies
    {
        // Project policies
        public const string ProjectOwner = "Owner";
        public const string ProjectAdmin = "Admin";
        public const string ProjectMember = "Member";
        public const string ProjectReader = "Reader";

        // System policy
        public const string SystemAdmin = "SystemAdmin";
    }
}
