using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignUserProjectConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_Slug",
                table: "Projects");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWSEQUENTIALID()");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Projects",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWSEQUENTIALID()");

            // Add OwnerId column as nullable first (to backfill data safely)
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: true);

            // Backfill step 1: set OwnerId from existing active Owner members
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.OwnerId = pm.UserId
                FROM Projects p
                INNER JOIN ProjectMembers pm
                    ON pm.ProjectId = p.Id
                   AND pm.Role = 0            -- Owner
                   AND pm.RemovedAt IS NULL
                WHERE p.OwnerId IS NULL;
            ");

            // Backfill step 2: where still null but there are active members, promote earliest active member to Owner
            migrationBuilder.Sql(@"
                ;WITH FirstActive AS (
                    SELECT p.Id AS ProjectId, MIN(pm2.JoinedAt) AS FirstJoined
                    FROM Projects p
                    INNER JOIN ProjectMembers pm2
                        ON pm2.ProjectId = p.Id
                       AND pm2.RemovedAt IS NULL
                    WHERE p.OwnerId IS NULL
                    GROUP BY p.Id
                )
                UPDATE pm
                SET pm.Role = 0               -- promote to Owner
                FROM ProjectMembers pm
                INNER JOIN FirstActive fa
                    ON fa.ProjectId = pm.ProjectId
                   AND pm.JoinedAt = fa.FirstJoined
                WHERE pm.RemovedAt IS NULL;
            ");

            // Backfill step 3: set OwnerId from the newly promoted Owner members
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.OwnerId = pm.UserId
                FROM Projects p
                INNER JOIN ProjectMembers pm
                    ON pm.ProjectId = p.Id
                   AND pm.Role = 0
                   AND pm.RemovedAt IS NULL
                WHERE p.OwnerId IS NULL;
            ");

            // Guard: if some projects still have no OwnerId (no members to promote), fail explicitly
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM Projects WHERE OwnerId IS NULL)
                    THROW 51000, 'Found Projects without OwnerId after backfill (no members to promote).', 1;
            ");

            // Remove default constraint on ProjectMembers.JoinedAt (domain sets it)
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "JoinedAt",
                table: "ProjectMembers",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Email_NotEmpty",
                table: "Users",
                sql: "[Email] <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Name_NotEmpty",
                table: "Users",
                sql: "[Name] <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_PasswordHash_Length",
                table: "Users",
                sql: "DATALENGTH([PasswordHash]) = 32");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_PasswordSalt_Length",
                table: "Users",
                sql: "DATALENGTH([PasswordSalt]) = 16");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerId_Slug",
                table: "Projects",
                columns: new[] { "OwnerId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId",
                table: "ProjectMembers",
                column: "ProjectId",
                unique: true,
                filter: "[Role] = 0 AND [RemovedAt] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_OwnerId",
                table: "Projects",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_OwnerId",
                table: "Projects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Email_NotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Name_NotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_PasswordHash_Length",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_PasswordSalt_Length",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerId_Slug",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_ProjectId",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Projects");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWSEQUENTIALID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Projects",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWSEQUENTIALID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "JoinedAt",
                table: "ProjectMembers",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Slug",
                table: "Projects",
                column: "Slug",
                unique: true);
        }

    }
}
