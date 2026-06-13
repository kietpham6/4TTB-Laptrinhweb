using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Thitructuyen.Migrations
{
    public partial class AddExamPassword : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExamPassword",
                table: "Exams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExamPassword",
                table: "Exams");
        }
    }
}
