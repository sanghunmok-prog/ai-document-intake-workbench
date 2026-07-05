using AiDocumentIntakeWorkbench.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiDocumentIntakeWorkbench.Api.Data.Configurations;

public sealed class ExtractedDocumentFieldConfiguration : IEntityTypeConfiguration<ExtractedDocumentField>
{
    public void Configure(EntityTypeBuilder<ExtractedDocumentField> builder)
    {
        builder.ToTable("ExtractedDocumentFields");

        builder.HasKey(field => field.Id);

        builder.Property(field => field.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(field => field.Value)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(field => field.Confidence)
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(field => field.CreatedUtc)
            .IsRequired();

        builder.HasIndex(field => field.DocumentProcessingResultId);

        builder.HasOne(field => field.DocumentProcessingResult)
            .WithMany()
            .HasForeignKey(field => field.DocumentProcessingResultId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
