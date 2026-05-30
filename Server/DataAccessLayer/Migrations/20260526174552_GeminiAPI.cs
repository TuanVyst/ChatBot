using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class GeminiAPI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VectorJson",
                table: "DocumentChunks");

            migrationBuilder.AlterColumn<string>(
                name: "IndexStatus",
                table: "Documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ChapterName",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "DocumentChunks",
                type: "vector(768)",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "DocumentChunks");

            migrationBuilder.AlterColumn<string>(
                name: "IndexStatus",
                table: "Documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "Documents",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ChapterName",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "VectorJson",
                table: "DocumentChunks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
