using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiDocumentIntakeWorkbench.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiProcessingResultsAndValidationFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentProcessingResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntakeDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSampleDocumentId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OverallConfidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    SuggestedRouting = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Rationale = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentProcessingResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentProcessingResults_IntakeDocuments_IntakeDocumentId",
                        column: x => x.IntakeDocumentId,
                        principalTable: "IntakeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtractedDocumentFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentProcessingResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedDocumentFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractedDocumentFields_DocumentProcessingResults_DocumentProcessingResultId",
                        column: x => x.DocumentProcessingResultId,
                        principalTable: "DocumentProcessingResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntakeDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentProcessingResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlagType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationFlags_DocumentProcessingResults_DocumentProcessingResultId",
                        column: x => x.DocumentProcessingResultId,
                        principalTable: "DocumentProcessingResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValidationFlags_IntakeDocuments_IntakeDocumentId",
                        column: x => x.IntakeDocumentId,
                        principalTable: "IntakeDocuments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentProcessingResults_IntakeDocumentId",
                table: "DocumentProcessingResults",
                column: "IntakeDocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedDocumentFields_DocumentProcessingResultId",
                table: "ExtractedDocumentFields",
                column: "DocumentProcessingResultId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationFlags_DocumentProcessingResultId",
                table: "ValidationFlags",
                column: "DocumentProcessingResultId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationFlags_IntakeDocumentId",
                table: "ValidationFlags",
                column: "IntakeDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExtractedDocumentFields");

            migrationBuilder.DropTable(
                name: "ValidationFlags");

            migrationBuilder.DropTable(
                name: "DocumentProcessingResults");
        }
    }
}
