using AiDocumentIntakeWorkbench.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiDocumentIntakeWorkbench.Api.Data;

public sealed class WorkbenchDbContext(DbContextOptions<WorkbenchDbContext> options) : DbContext(options)
{
    public const string ConnectionStringName = "WorkbenchDb";

    public const string LocalPlaceholderConnectionString =
        "Server=localhost,1433;Database=AiDocumentIntakeWorkbench;Integrated Security=false;Encrypt=True;TrustServerCertificate=True";

    public DbSet<IntakeDocument> IntakeDocuments => Set<IntakeDocument>();

    public DbSet<ReviewState> ReviewStates => Set<ReviewState>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<DocumentProcessingResult> DocumentProcessingResults => Set<DocumentProcessingResult>();

    public DbSet<ExtractedDocumentField> ExtractedDocumentFields => Set<ExtractedDocumentField>();

    public DbSet<ValidationFlag> ValidationFlags => Set<ValidationFlag>();

    public static string ResolveConnectionString(string? configuredConnectionString)
    {
        return string.IsNullOrWhiteSpace(configuredConnectionString)
            ? LocalPlaceholderConnectionString
            : configuredConnectionString;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkbenchDbContext).Assembly);
    }
}
