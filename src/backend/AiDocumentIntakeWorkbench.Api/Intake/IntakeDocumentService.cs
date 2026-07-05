using AiDocumentIntakeWorkbench.Api.Data;
using AiDocumentIntakeWorkbench.Api.Domain;
using AiDocumentIntakeWorkbench.Api.SampleDocuments;
using Microsoft.EntityFrameworkCore;

namespace AiDocumentIntakeWorkbench.Api.Intake;

public sealed class IntakeDocumentService(
    WorkbenchDbContext dbContext,
    ISampleDocumentCatalog sampleDocumentCatalog)
{
    public async Task<CreateIntakeDocumentResult> CreateFromSampleAsync(
        string? sampleDocumentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sampleDocumentId))
        {
            return CreateIntakeDocumentResult.Failure(CreateIntakeDocumentError.MissingSampleDocumentId);
        }

        var sampleDocument = sampleDocumentCatalog.FindById(sampleDocumentId.Trim());
        if (sampleDocument is null)
        {
            return CreateIntakeDocumentResult.Failure(CreateIntakeDocumentError.SampleDocumentNotFound);
        }

        var intakeDocument = new IntakeDocument(
            sampleDocument.Title,
            sampleDocument.Id,
            sampleDocument.Scenario,
            sampleDocument.Summary,
            sampleDocument.Content);

        dbContext.IntakeDocuments.Add(intakeDocument);
        dbContext.AuditEvents.Add(new AuditEvent(
            intakeDocument.Id,
            "SampleDocumentSelected",
            $"Sample document '{sampleDocument.Id}' selected for intake."));

        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateIntakeDocumentResult.Success(intakeDocument);
    }

    public async Task<IReadOnlyList<IntakeDocument>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.IntakeDocuments
            .AsNoTracking()
            .OrderByDescending(document => document.CreatedUtc)
            .ToListAsync(cancellationToken);
    }
}
