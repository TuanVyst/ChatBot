using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSubscriptionToTokenLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DailyQuestionLimit",
                table: "SubscriptionPlans",
                newName: "DailyTokenLimit");

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DailyTokenLimit", "Description" },
                values: new object[] { 50000, "Giới hạn 50,000 tokens/ngày trong 7 ngày" });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DailyTokenLimit", "Description" },
                values: new object[] { 50000, "Giới hạn 50,000 tokens/ngày trong 30 ngày" });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DailyTokenLimit", "Description" },
                values: new object[] { 50000, "Giới hạn 50,000 tokens/ngày trong 365 ngày" });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 18, 2, 12, 37, 114, DateTimeKind.Utc).AddTicks(4006));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DailyTokenLimit",
                table: "SubscriptionPlans",
                newName: "DailyQuestionLimit");

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DailyQuestionLimit", "Description" },
                values: new object[] { 10, "Hỏi 10 câu/ngày trong 7 ngày" });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DailyQuestionLimit", "Description" },
                values: new object[] { 10, "Hỏi 10 câu/ngày trong 30 ngày" });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DailyQuestionLimit", "Description" },
                values: new object[] { 10, "Hỏi 10 câu/ngày trong 365 ngày" });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 7, 18, 1, 58, 26, 482, DateTimeKind.Utc).AddTicks(6528));
        }
    }
}
