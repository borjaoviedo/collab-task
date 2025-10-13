using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProjectBoardSchemaUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_ProjectMembers_ProjectId",
                table: "ProjectMembers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProjectMembers_Role",
                table: "ProjectMembers");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "Users",
                newName: "UX_Users_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_OwnerId_Slug",
                table: "Projects",
                newName: "UX_Projects_OwnerId_Slug");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "ProjectMembers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "Lanes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lanes", x => x.Id);
                    table.CheckConstraint("CK_Lanes_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Lanes_Order_NonNegative", "[Order] >= 0");
                    table.ForeignKey(
                        name: "FK_Lanes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Columns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LaneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Columns", x => x.Id);
                    table.CheckConstraint("CK_Columns_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Columns_Order_NonNegative", "[Order] >= 0");
                    table.ForeignKey(
                        name: "FK_Columns_Lanes_LaneId",
                        column: x => x.LaneId,
                        principalTable: "Lanes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColumnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LaneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SortKey = table.Column<decimal>(type: "decimal(18,6)", nullable: false, defaultValue: 0m),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.CheckConstraint("CK_Tasks_Description_NotEmpty", "LEN(LTRIM(RTRIM([Description]))) > 0");
                    table.CheckConstraint("CK_Tasks_SortKey_NonNegative", "[SortKey] >= 0");
                    table.CheckConstraint("CK_Tasks_Title_NotEmpty", "LEN(LTRIM(RTRIM([Title]))) > 0");
                    table.ForeignKey(
                        name: "FK_Tasks_Columns_ColumnId",
                        column: x => x.ColumnId,
                        principalTable: "Columns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => new { x.TaskId, x.UserId });
                    table.ForeignKey(
                        name: "FK_Assignments_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.CheckConstraint("CK_Notes_Content_NotEmpty", "LEN(LTRIM(RTRIM([Content]))) > 0");
                    table.ForeignKey(
                        name: "FK_Notes_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notes_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskActivities", x => x.Id);
                    table.CheckConstraint("CK_TaskActivities_Payload_NotEmpty", "LEN(LTRIM(RTRIM([Payload]))) > 0");
                    table.ForeignKey(
                        name: "FK_TaskActivities_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskActivities_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Email_NotEmpty",
                table: "Users",
                sql: "LEN(LTRIM(RTRIM([Email]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Name_NotEmpty",
                table: "Users",
                sql: "LEN(LTRIM(RTRIM([Name]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_PasswordHash_Length_32",
                table: "Users",
                sql: "DATALENGTH([PasswordHash]) = 32");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_PasswordSalt_Length_16",
                table: "Users",
                sql: "DATALENGTH([PasswordSalt]) = 16");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerId",
                table: "Projects",
                column: "OwnerId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Projects_Name_NotEmpty",
                table: "Projects",
                sql: "LEN(LTRIM(RTRIM([Name]))) > 0");

            migrationBuilder.CreateIndex(
                name: "UX_ProjectMembers_ProjectId_ActiveOwner",
                table: "ProjectMembers",
                column: "ProjectId",
                unique: true,
                filter: "[Role] = 'Owner' AND [RemovedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TaskId",
                table: "Assignments",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_UserId",
                table: "Assignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_Assignments_Task_Owner",
                table: "Assignments",
                columns: new[] { "TaskId", "Role" },
                unique: true,
                filter: "[Role] = 'Owner'");

            migrationBuilder.CreateIndex(
                name: "IX_Columns_ProjectId",
                table: "Columns",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "UX_Columns_LaneId_Name",
                table: "Columns",
                columns: new[] { "LaneId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Columns_LaneId_Order",
                table: "Columns",
                columns: new[] { "LaneId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Lanes_ProjectId_Name",
                table: "Lanes",
                columns: new[] { "ProjectId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Lanes_ProjectId_Order",
                table: "Lanes",
                columns: new[] { "ProjectId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notes_AuthorId",
                table: "Notes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_TaskId_CreatedAt",
                table: "Notes",
                columns: new[] { "TaskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskActivities_ActorId",
                table: "TaskActivities",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskActivities_TaskId_CreatedAt",
                table: "TaskActivities",
                columns: new[] { "TaskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ColumnId_SortKey",
                table: "Tasks",
                columns: new[] { "ColumnId", "SortKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_LaneId",
                table: "Tasks",
                column: "LaneId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "TaskActivities");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "Columns");

            migrationBuilder.DropTable(
                name: "Lanes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Email_NotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Name_NotEmpty",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_PasswordHash_Length_32",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_PasswordSalt_Length_16",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerId",
                table: "Projects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Projects_Name_NotEmpty",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "UX_ProjectMembers_ProjectId_ActiveOwner",
                table: "ProjectMembers");

            migrationBuilder.RenameIndex(
                name: "UX_Users_Email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.RenameIndex(
                name: "UX_Projects_OwnerId_Slug",
                table: "Projects",
                newName: "IX_Projects_OwnerId_Slug");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "ProjectMembers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

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
                name: "IX_ProjectMembers_ProjectId",
                table: "ProjectMembers",
                column: "ProjectId",
                unique: true,
                filter: "[Role] = 0 AND [RemovedAt] IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProjectMembers_Role",
                table: "ProjectMembers",
                sql: "[Role] IN (0,1,2,3)");
        }
    }
}
