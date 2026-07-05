using AiDocumentIntakeWorkbench.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiDocumentIntakeWorkbench.Api.Data.Configurations;

public sealed class IntakeDocumentConfiguration : IEntityTypeConfiguration<IntakeDocument>
{
    public void Configure(EntityTypeBuilder<IntakeDocument> builder)
    {
        builder.ToTable("IntakeDocuments");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.DisplayName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(document => document.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(document => document.CreatedUtc)
            .IsRequired();

        builder.Property(document => document.UpdatedUtc)
            .IsRequired();

        builder.Navigation(document => document.AuditEvents)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(document => document.ReviewState)
            .WithOne(reviewState => reviewState.IntakeDocument)
            .HasForeignKey<ReviewState>(reviewState => reviewState.IntakeDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(document => document.AuditEvents)
            .WithOne(auditEvent => auditEvent.IntakeDocument)
            .HasForeignKey(auditEvent => auditEvent.IntakeDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
