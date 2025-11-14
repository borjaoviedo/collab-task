using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInvitedAt_AddChangeRole_ProjectMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ProjectMembers_InvitedAt_Before_JoinedAt",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "InvitedAt",
                table: "ProjectMembers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InvitedAt",
                table: "ProjectMembers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProjectMembers_InvitedAt_Before_JoinedAt",
                table: "ProjectMembers",
                sql: "[InvitedAt] IS NULL OR [InvitedAt] <= [JoinedAt]");
        }
    }
}
