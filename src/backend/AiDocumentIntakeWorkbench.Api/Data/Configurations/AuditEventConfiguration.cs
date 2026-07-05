using AiDocumentIntakeWorkbench.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiDocumentIntakeWorkbench.Api.Data.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");

        builder.HasKey(auditEvent => auditEvent.Id);

        builder.Property(auditEvent => auditEvent.EventType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(auditEvent => auditEvent.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(auditEvent => auditEvent.CreatedUtc)
            .IsRequired();

        builder.HasIndex(auditEvent => auditEvent.IntakeDocumentId);
    }
}
