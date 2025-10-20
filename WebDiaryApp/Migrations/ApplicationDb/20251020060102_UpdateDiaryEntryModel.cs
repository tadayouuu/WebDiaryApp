using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebDiaryApp.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class UpdateDiaryEntryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "DiaryEntries");

            migrationBuilder.RenameColumn(
                name: "Tag",
                table: "DiaryEntries",
                newName: "Category");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DiaryEntries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DiaryEntries");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "DiaryEntries",
                newName: "Tag");

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "DiaryEntries",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
