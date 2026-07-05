using AiDocumentIntakeWorkbench.Api.Ai;
using AiDocumentIntakeWorkbench.Api.Data;
using AiDocumentIntakeWorkbench.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiDocumentIntakeWorkbench.Api.AiProcessing;

public sealed class AiProcessingService(
    WorkbenchDbContext dbContext,
    IDocumentAiProcessor documentAiProcessor)
{
    public async Task<ProcessIntakeDocumentResult> ProcessAsync(
        Guid intakeDocumentId,
        CancellationToken cancellationToken = default)
    {
        var intakeDocument = await dbContext.IntakeDocuments
            .FirstOrDefaultAsync(document => document.Id == intakeDocumentId, cancellationToken);

        if (intakeDocument is null)
        {
            return ProcessIntakeDocumentResult.Failure(
                ProcessIntakeDocumentError.DocumentNotFound,
                "Intake document was not found.");
        }

        var alreadyProcessed = await dbContext.DocumentProcessingResults
            .AnyAsync(result => result.IntakeDocumentId == intakeDocumentId, cancellationToken);

        if (alreadyProcessed)
        {
            return ProcessIntakeDocumentResult.Failure(
                ProcessIntakeDocumentError.AlreadyProcessed,
                "Intake document has already been processed.");
        }

        if (intakeDocument.Status != WorkflowStatus.Received)
        {
            return ProcessIntakeDocumentResult.Failure(
                ProcessIntakeDocumentError.InvalidWorkflowStatus,
                $"Intake document cannot be processed from status '{intakeDocument.Status}'.");
        }

        var aiResult = await documentAiProcessor.ProcessAsync(
            new DocumentAiInput(
                intakeDocument.SampleDocumentId,
                intakeDocument.Scenario,
                intakeDocument.DisplayName,
                intakeDocument.Summary,
                intakeDocument.DocumentText),
            cancellationToken);

        if (!aiResult.Succeeded || aiResult.Output is null)
        {
            return ProcessIntakeDocumentResult.Failure(
                ProcessIntakeDocumentError.AiProcessingFailed,
                $"AI processing failed with error '{aiResult.Error}'.");
        }

        if (dbContext.Database.IsRelational())
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var transactionResult = await PersistProcessingResultAsync(
                intakeDocument,
                aiResult.Output,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return transactionResult;
        }

        return await PersistProcessingResultAsync(intakeDocument, aiResult.Output, cancellationToken);
    }

    private async Task<ProcessIntakeDocumentResult> PersistProcessingResultAsync(
        IntakeDocument intakeDocument,
        DocumentAiOutput output,
        CancellationToken cancellationToken)
    {
        var originalStatus = intakeDocument.Status;
        var processingResult = new DocumentProcessingResult(
            intakeDocument.Id,
            output.SourceSampleDocumentId,
            output.DocumentType,
            output.OverallConfidence,
            output.SuggestedRouting,
            output.Rationale);

        var extractedFields = output.Fields
            .Select(field => new ExtractedDocumentField(
                processingResult.Id,
                field.Name,
                field.Value,
                field.Confidence))
            .ToArray();

        var validationFlags = AiProcessingValidation
            .CreateFlags(intakeDocument.Id, processingResult.Id, output)
            .ToArray();

        dbContext.DocumentProcessingResults.Add(processingResult);
        dbContext.ExtractedDocumentFields.AddRange(extractedFields);
        dbContext.ValidationFlags.AddRange(validationFlags);

        dbContext.AuditEvents.Add(new AuditEvent(
            intakeDocument.Id,
            "AiProcessingCompleted",
            $"AI processing completed with document type '{output.DocumentType}' and confidence {output.OverallConfidence:0.00}."));

        if (validationFlags.Length > 0)
        {
            dbContext.AuditEvents.Add(new AuditEvent(
                intakeDocument.Id,
                "ValidationFlagsCreated",
                $"{validationFlags.Length} validation flag(s) created from backend validation."));
        }

        if (await dbContext.ReviewStates.AllAsync(
                reviewState => reviewState.IntakeDocumentId != intakeDocument.Id,
                cancellationToken))
        {
            dbContext.ReviewStates.Add(new ReviewState(intakeDocument.Id));
        }

        if (intakeDocument.ChangeStatus(WorkflowStatus.AwaitingReview))
        {
            dbContext.AuditEvents.Add(new AuditEvent(
                intakeDocument.Id,
                "WorkflowStatusChanged",
                $"Workflow status changed from '{originalStatus}' to '{intakeDocument.Status}'."));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ProcessIntakeDocumentResult.Success(new ProcessedIntakeDocumentSummary(
            intakeDocument.Id,
            intakeDocument.Status.ToString(),
            processingResult.DocumentType,
            processingResult.OverallConfidence,
            extractedFields
                .Select(field => new ProcessedFieldSummary(field.Name, field.Value, field.Confidence))
                .ToArray(),
            validationFlags
                .Select(flag => new ValidationFlagSummary(
                    flag.FlagType.ToString(),
                    flag.Severity.ToString(),
                    flag.FieldName,
                    flag.Message))
                .ToArray(),
            processingResult.SuggestedRouting,
            processingResult.Rationale));
    }
}
