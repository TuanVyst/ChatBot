using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace DataAccessLayer.Migrations
{
    public partial class AddChapterAndIndexStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChapterName",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Documents",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "IndexStatus",
                table: "Documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChapterName",
                table: "Documents");
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Documents");
            migrationBuilder.DropColumn(
                name: "IndexStatus",
                table: "Documents");
        }
    }
}
