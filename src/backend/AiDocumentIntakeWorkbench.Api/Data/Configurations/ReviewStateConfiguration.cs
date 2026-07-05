using AiDocumentIntakeWorkbench.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiDocumentIntakeWorkbench.Api.Data.Configurations;

public sealed class ReviewStateConfiguration : IEntityTypeConfiguration<ReviewState>
{
    public void Configure(EntityTypeBuilder<ReviewState> builder)
    {
        builder.ToTable("ReviewStates");

        builder.HasKey(reviewState => reviewState.Id);

        builder.Property(reviewState => reviewState.RequiresHumanReview)
            .IsRequired();

        builder.Property(reviewState => reviewState.CreatedUtc)
            .IsRequired();

        builder.Property(reviewState => reviewState.UpdatedUtc)
            .IsRequired();

        builder.HasIndex(reviewState => reviewState.IntakeDocumentId)
            .IsUnique();
    }
}
