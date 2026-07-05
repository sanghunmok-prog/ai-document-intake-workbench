using AiDocumentIntakeWorkbench.Api.Data;
using AiDocumentIntakeWorkbench.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiDocumentIntakeWorkbench.Api.Tests.Data;

public sealed class WorkbenchDbContextTests
{
    [Fact]
    public void Model_IncludesPr02Entities()
    {
        using var context = CreateContext();

        Assert.NotNull(context.Model.FindEntityType(typeof(IntakeDocument)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ReviewState)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AuditEvent)));
    }

    [Fact]
    public void Model_ConfiguresDocumentRelationships()
    {
        using var context = CreateContext();

        var reviewStateEntity = context.Model.FindEntityType(typeof(ReviewState));
        var auditEventEntity = context.Model.FindEntityType(typeof(AuditEvent));

        Assert.NotNull(reviewStateEntity);
        Assert.NotNull(auditEventEntity);

        Assert.Contains(
            reviewStateEntity.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(IntakeDocument)
                && foreignKey.IsUnique);

        Assert.Contains(
            auditEventEntity.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(IntakeDocument)
                && !foreignKey.IsUnique);
    }

    [Fact]
    public void ResolveConnectionString_UsesConfiguredValueWhenPresent()
    {
        const string configured = "Server=example;Database=Configured;";

        var resolved = WorkbenchDbContext.ResolveConnectionString(configured);

        Assert.Equal(configured, resolved);
    }

    [Fact]
    public void ResolveConnectionString_UsesPublicSafeLocalPlaceholderWhenMissing()
    {
        var resolved = WorkbenchDbContext.ResolveConnectionString(" ");

        Assert.Equal(WorkbenchDbContext.LocalPlaceholderConnectionString, resolved);
    }

    private static WorkbenchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WorkbenchDbContext>()
            .UseSqlServer(WorkbenchDbContext.LocalPlaceholderConnectionString)
            .Options;

        return new WorkbenchDbContext(options);
    }
}
