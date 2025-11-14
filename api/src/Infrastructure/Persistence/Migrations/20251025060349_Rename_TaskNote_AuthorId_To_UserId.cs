using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rename_TaskNote_AuthorId_To_UserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Users_AuthorId",
                table: "Notes");

            migrationBuilder.RenameColumn(
                name: "AuthorId",
                table: "Notes",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Notes_AuthorId",
                table: "Notes",
                newName: "IX_Notes_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Users_UserId",
                table: "Notes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Users_UserId",
                table: "Notes");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Notes",
                newName: "AuthorId");

            migrationBuilder.RenameIndex(
                name: "IX_Notes_UserId",
                table: "Notes",
                newName: "IX_Notes_AuthorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Users_AuthorId",
                table: "Notes",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
