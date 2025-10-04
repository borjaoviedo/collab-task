using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNameToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
            WITH Base AS (
              SELECT Id,
                     CASE 
                       WHEN CHARINDEX('@', Email) > 1 
                         THEN LOWER(SUBSTRING(Email, 1, CHARINDEX('@', Email) - 1))
                       ELSE LOWER(Email)
                     END AS BaseName
              FROM Users
            ),
            Numbered AS (
              SELECT b.Id,
                     b.BaseName,
                     ROW_NUMBER() OVER (PARTITION BY b.BaseName ORDER BY b.Id) AS rn
              FROM Base b
            )
            UPDATE u
            SET Name = 
              CASE WHEN n.rn = 1 THEN n.BaseName ELSE CONCAT(n.BaseName, '-', n.rn) END
            FROM Users u
            JOIN Numbered n ON u.Id = n.Id;
            """);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Users",
                type: "nvarchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Name",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");
        }
    }
}
