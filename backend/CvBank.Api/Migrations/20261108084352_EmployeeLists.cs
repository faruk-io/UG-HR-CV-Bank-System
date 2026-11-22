using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvBank.Api.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JoiningRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ConcernId = table.Column<int>(type: "int", nullable: true),
                    DateOfJoining = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProbationPeriod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AfterProbationSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EffectiveSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoiningRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JoiningRecords_Concerns_ConcernId",
                        column: x => x.ConcernId,
                        principalTable: "Concerns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JoiningRecords_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ResignedRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConcernId = table.Column<int>(type: "int", nullable: true),
                    DateOfJoining = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResignDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastWorkingDay = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NoticePeriod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HrApproved = table.Column<bool>(type: "bit", nullable: false),
                    CauseOfResignation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResignedRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResignedRecords_Concerns_ConcernId",
                        column: x => x.ConcernId,
                        principalTable: "Concerns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ResignedRecords_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JoiningRecords_ConcernId",
                table: "JoiningRecords",
                column: "ConcernId");

            migrationBuilder.CreateIndex(
                name: "IX_JoiningRecords_CreatedById",
                table: "JoiningRecords",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_JoiningRecords_DateOfJoining",
                table: "JoiningRecords",
                column: "DateOfJoining");

            migrationBuilder.CreateIndex(
                name: "IX_JoiningRecords_EmployeeId",
                table: "JoiningRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_JoiningRecords_Name",
                table: "JoiningRecords",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ResignedRecords_ConcernId",
                table: "ResignedRecords",
                column: "ConcernId");

            migrationBuilder.CreateIndex(
                name: "IX_ResignedRecords_CreatedById",
                table: "ResignedRecords",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ResignedRecords_EmployeeId",
                table: "ResignedRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResignedRecords_Name",
                table: "ResignedRecords",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ResignedRecords_ResignDate",
                table: "ResignedRecords",
                column: "ResignDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JoiningRecords");

            migrationBuilder.DropTable(
                name: "ResignedRecords");
        }
    }
}
