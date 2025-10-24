using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDiaryApp.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddUserIdToDiaryEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "DiaryEntries",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "DiaryEntries");
        }
    }
}
