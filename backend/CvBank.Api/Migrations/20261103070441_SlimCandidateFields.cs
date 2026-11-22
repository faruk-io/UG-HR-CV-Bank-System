using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvBank.Api.Migrations
{
    /// <inheritdoc />
    public partial class SlimCandidateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcademicQualification",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "PermanentAddress",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "PermanentArea",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "PermanentDistrict",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "PresentArea",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "PresentDistrict",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "Candidates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicQualification",
                table: "Candidates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermanentAddress",
                table: "Candidates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermanentArea",
                table: "Candidates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermanentDistrict",
                table: "Candidates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PresentArea",
                table: "Candidates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PresentDistrict",
                table: "Candidates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Candidates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "Candidates",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
