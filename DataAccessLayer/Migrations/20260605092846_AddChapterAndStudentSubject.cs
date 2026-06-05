using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddChapterAndStudentSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Accounts_TeacherAccount_id",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_TeacherAccount_id",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "TeacherAccount_id",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "ChapterName",
                table: "Documents");

            migrationBuilder.AddColumn<int>(
                name: "ChapterId",
                table: "Documents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapters_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentSubjects_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_LectureAccountId",
                table: "Subjects",
                column: "LectureAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ChapterId",
                table: "Documents",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_SubjectId",
                table: "Chapters",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSubjects_AccountId",
                table: "StudentSubjects",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSubjects_SubjectId",
                table: "StudentSubjects",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Chapters_ChapterId",
                table: "Documents",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Accounts_LectureAccountId",
                table: "Subjects",
                column: "LectureAccountId",
                principalTable: "Accounts",
                principalColumn: "Account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Chapters_ChapterId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Accounts_LectureAccountId",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "StudentSubjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_LectureAccountId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ChapterId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "Documents");

            migrationBuilder.AddColumn<Guid>(
                name: "TeacherAccount_id",
                table: "Subjects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChapterName",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_TeacherAccount_id",
                table: "Subjects",
                column: "TeacherAccount_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Accounts_TeacherAccount_id",
                table: "Subjects",
                column: "TeacherAccount_id",
                principalTable: "Accounts",
                principalColumn: "Account_id");
        }
    }
}
