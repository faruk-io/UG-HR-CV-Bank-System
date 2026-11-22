using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvBank.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignFormAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "District",
                table: "Candidates",
                newName: "PresentDistrict");

            migrationBuilder.RenameColumn(
                name: "Area",
                table: "Candidates",
                newName: "PresentArea");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermanentArea",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "PermanentDistrict",
                table: "Candidates");

            migrationBuilder.RenameColumn(
                name: "PresentDistrict",
                table: "Candidates",
                newName: "District");

            migrationBuilder.RenameColumn(
                name: "PresentArea",
                table: "Candidates",
                newName: "Area");
        }
    }
}
