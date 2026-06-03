using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherToSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TeacherAccountId",
                table: "Subjects",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_TeacherAccountId",
                table: "Subjects",
                column: "TeacherAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Accounts_TeacherAccountId",
                table: "Subjects",
                column: "TeacherAccountId",
                principalTable: "Accounts",
                principalColumn: "Account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Accounts_TeacherAccountId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_TeacherAccountId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "TeacherAccountId",
                table: "Subjects");
        }
    }
}
