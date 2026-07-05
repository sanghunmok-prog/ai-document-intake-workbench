using AiDocumentIntakeWorkbench.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiDocumentIntakeWorkbench.Api.Data.Configurations;

public sealed class ValidationFlagConfiguration : IEntityTypeConfiguration<ValidationFlag>
{
    public void Configure(EntityTypeBuilder<ValidationFlag> builder)
    {
        builder.ToTable("ValidationFlags");

        builder.HasKey(flag => flag.Id);

        builder.Property(flag => flag.FlagType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(flag => flag.Severity)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(flag => flag.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(flag => flag.FieldName)
            .HasMaxLength(128);

        builder.Property(flag => flag.CreatedUtc)
            .IsRequired();

        builder.HasIndex(flag => flag.IntakeDocumentId);

        builder.HasIndex(flag => flag.DocumentProcessingResultId);

        builder.HasOne(flag => flag.IntakeDocument)
            .WithMany()
            .HasForeignKey(flag => flag.IntakeDocumentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(flag => flag.DocumentProcessingResult)
            .WithMany()
            .HasForeignKey(flag => flag.DocumentProcessingResultId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
