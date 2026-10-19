using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FCQI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdvisorTutorKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TutorKind",
                table: "advisors",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Alumno tutor")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TutorKind",
                table: "advisors");
        }
    }
}
