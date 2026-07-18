using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupEmbeddingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackupEmbeddingModel",
                table: "SystemSettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BackupEmbeddingModel", "UpdatedAt" },
                values: new object[] { "gemini-embedding-2-preview", new DateTime(2026, 7, 18, 2, 17, 57, 70, DateTimeKind.Utc).AddTicks(1553) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackupEmbeddingModel",
                table: "SystemSettings");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 18, 2, 12, 37, 114, DateTimeKind.Utc).AddTicks(4006));
        }
    }
}
