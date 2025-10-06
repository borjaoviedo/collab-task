namespace Api.Auth.Authorization
{
    public static class Policies
    {
        public const string ProjectOwner = "Owner";
        public const string ProjectAdmin = "Admin";
        public const string ProjectMember = "Member";
        public const string ProjectReader = "Reader";

        public const string SystemAdmin = "SystemAdmin";
    }
}
