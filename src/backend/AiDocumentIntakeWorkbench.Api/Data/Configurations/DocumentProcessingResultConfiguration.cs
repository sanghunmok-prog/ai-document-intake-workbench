using AiDocumentIntakeWorkbench.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiDocumentIntakeWorkbench.Api.Data.Configurations;

public sealed class DocumentProcessingResultConfiguration : IEntityTypeConfiguration<DocumentProcessingResult>
{
    public void Configure(EntityTypeBuilder<DocumentProcessingResult> builder)
    {
        builder.ToTable("DocumentProcessingResults");

        builder.HasKey(result => result.Id);

        builder.Property(result => result.SourceSampleDocumentId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(result => result.DocumentType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(result => result.OverallConfidence)
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(result => result.SuggestedRouting)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(result => result.Rationale)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(result => result.CreatedUtc)
            .IsRequired();

        builder.HasIndex(result => result.IntakeDocumentId)
            .IsUnique();

        builder.HasOne(result => result.IntakeDocument)
            .WithMany()
            .HasForeignKey(result => result.IntakeDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
