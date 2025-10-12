using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProjectBoard_AssignmentsRowVersion_ProviderChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "TaskActivities",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Assignments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assignments_Role_NotEmpty",
                table: "Assignments",
                sql: "LEN(LTRIM(RTRIM([Role]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assignments_Role_Valid",
                table: "Assignments",
                sql: "[Role] IN ('Owner','CoOwner')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Assignments_Role_NotEmpty",
                table: "Assignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assignments_Role_Valid",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Assignments");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "TaskActivities",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");
        }
    }
}
