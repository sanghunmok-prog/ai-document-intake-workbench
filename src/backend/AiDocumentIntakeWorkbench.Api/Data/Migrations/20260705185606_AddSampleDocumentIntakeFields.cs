using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiDocumentIntakeWorkbench.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSampleDocumentIntakeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentText",
                table: "IntakeDocuments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SampleDocumentId",
                table: "IntakeDocuments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scenario",
                table: "IntakeDocuments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "IntakeDocuments",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentText",
                table: "IntakeDocuments");

            migrationBuilder.DropColumn(
                name: "SampleDocumentId",
                table: "IntakeDocuments");

            migrationBuilder.DropColumn(
                name: "Scenario",
                table: "IntakeDocuments");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "IntakeDocuments");
        }
    }
}
