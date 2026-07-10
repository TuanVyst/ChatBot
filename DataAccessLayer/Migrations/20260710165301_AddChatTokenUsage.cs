using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddChatTokenUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionTokens",
                table: "ChatHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "ChatHistories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PromptTokens",
                table: "ChatHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalTokens",
                table: "ChatHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 10, 16, 53, 0, 683, DateTimeKind.Utc).AddTicks(8110));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionTokens",
                table: "ChatHistories");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "ChatHistories");

            migrationBuilder.DropColumn(
                name: "PromptTokens",
                table: "ChatHistories");

            migrationBuilder.DropColumn(
                name: "TotalTokens",
                table: "ChatHistories");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 10, 15, 23, 57, 641, DateTimeKind.Utc).AddTicks(5604));
        }
    }
}
