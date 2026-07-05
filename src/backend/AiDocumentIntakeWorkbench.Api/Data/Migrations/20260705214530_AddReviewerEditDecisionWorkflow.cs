using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiDocumentIntakeWorkbench.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewerEditDecisionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DecidedBy",
                table: "ReviewStates",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DecidedUtc",
                table: "ReviewStates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Decision",
                table: "ReviewStates",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "ExtractedDocumentFields",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedUtc",
                table: "ExtractedDocumentFields",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedValue",
                table: "ExtractedDocumentFields",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DecidedBy",
                table: "ReviewStates");

            migrationBuilder.DropColumn(
                name: "DecidedUtc",
                table: "ReviewStates");

            migrationBuilder.DropColumn(
                name: "Decision",
                table: "ReviewStates");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "ExtractedDocumentFields");

            migrationBuilder.DropColumn(
                name: "ReviewedUtc",
                table: "ExtractedDocumentFields");

            migrationBuilder.DropColumn(
                name: "ReviewedValue",
                table: "ExtractedDocumentFields");
        }
    }
}
