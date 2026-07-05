using AiDocumentIntakeWorkbench.Api.Data;
using AiDocumentIntakeWorkbench.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AiDocumentIntakeWorkbench.Api.Api;

public static class ReviewQueueEndpoints
{
    public static IEndpointRouteBuilder MapReviewQueueEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/review-queue", ListAsync);
        app.MapGet("/api/review-queue/{id:guid}", GetDetailAsync);

        return app;
    }

    public static async Task<Ok<ReviewQueueItemResponse[]>> ListAsync(
        WorkbenchDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var rows = await (
                from document in dbContext.IntakeDocuments.AsNoTracking()
                join processingResult in dbContext.DocumentProcessingResults.AsNoTracking()
                    on document.Id equals processingResult.IntakeDocumentId
                where document.Status == WorkflowStatus.AwaitingReview
                orderby document.UpdatedUtc descending
                select new
                {
                    document.Id,
                    document.DisplayName,
                    document.Status,
                    document.SampleDocumentId,
                    document.Scenario,
                    document.UpdatedUtc,
                    processingResult.DocumentType,
                    processingResult.OverallConfidence
                })
            .ToListAsync(cancellationToken);

        var intakeDocumentIds = rows
            .Select(row => row.Id)
            .ToArray();

        var flags = await dbContext.ValidationFlags
            .AsNoTracking()
            .Where(flag => intakeDocumentIds.Contains(flag.IntakeDocumentId))
            .Select(flag => new
            {
                flag.IntakeDocumentId,
                flag.Severity
            })
            .ToListAsync(cancellationToken);

        var flagSummaries = flags
            .GroupBy(flag => flag.IntakeDocumentId)
            .ToDictionary(
                group => group.Key,
                group => new ValidationFlagSummary(
                    group.Count(),
                    group
                        .Select(flag => flag.Severity)
                        .OrderByDescending(severity => severity)
                        .First()));

        var response = rows
            .Select(row =>
            {
                flagSummaries.TryGetValue(row.Id, out var flagSummary);

                return new ReviewQueueItemResponse(
                    row.Id,
                    row.DisplayName,
                    row.Status.ToString(),
                    row.DocumentType,
                    row.OverallConfidence,
                    flagSummary?.Count ?? 0,
                    flagSummary?.HighestSeverity.ToString(),
                    row.SampleDocumentId,
                    row.Scenario,
                    row.UpdatedUtc);
            })
            .ToArray();

        return TypedResults.Ok(response);
    }

    public static async Task<IResult> GetDetailAsync(
        Guid id,
        WorkbenchDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var document = await dbContext.IntakeDocuments
            .AsNoTracking()
            .Where(document => document.Id == id)
            .Select(document => new
            {
                document.Id,
                document.DisplayName,
                document.SampleDocumentId,
                document.Scenario,
                document.Summary,
                document.DocumentText,
                document.Status,
                document.CreatedUtc,
                document.UpdatedUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Review queue item was not found."));
        }

        var processingResult = await dbContext.DocumentProcessingResults
            .AsNoTracking()
            .Where(result => result.IntakeDocumentId == id)
            .OrderByDescending(result => result.CreatedUtc)
            .Select(result => new
            {
                result.Id,
                result.DocumentType,
                result.OverallConfidence,
                result.SuggestedRouting,
                result.Rationale
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (processingResult is null)
        {
            return TypedResults.NotFound(new ErrorResponse("Review queue item was not found."));
        }

        var persistedReviewState = await dbContext.ReviewStates
            .AsNoTracking()
            .Where(state => state.IntakeDocumentId == id)
            .SingleOrDefaultAsync(cancellationToken);

        var reviewState = persistedReviewState is null
            ? null
            : new ReviewStateDetailResponse(
                persistedReviewState.RequiresHumanReview,
                persistedReviewState.Decision?.ToString(),
                persistedReviewState.DecidedBy,
                persistedReviewState.DecidedUtc,
                persistedReviewState.CreatedUtc,
                persistedReviewState.UpdatedUtc);

        var extractedFields = await dbContext.ExtractedDocumentFields
            .AsNoTracking()
            .Where(field => field.DocumentProcessingResultId == processingResult.Id)
            .OrderBy(field => field.Name)
            .Select(field => new ExtractedFieldDetailResponse(
                field.Name,
                field.Value,
                field.Confidence,
                field.ReviewedValue,
                field.ReviewedBy,
                field.ReviewedUtc))
            .ToArrayAsync(cancellationToken);

        var persistedFlags = await dbContext.ValidationFlags
            .AsNoTracking()
            .Where(flag => flag.DocumentProcessingResultId == processingResult.Id)
            .ToArrayAsync(cancellationToken);

        var validationFlags = persistedFlags
            .OrderByDescending(flag => flag.Severity)
            .ThenBy(flag => flag.FlagType)
            .Select(flag => new ValidationFlagDetailResponse(
                flag.FlagType.ToString(),
                flag.Severity.ToString(),
                flag.FieldName,
                flag.Message,
                flag.CreatedUtc))
            .ToArray();

        var auditEvents = await dbContext.AuditEvents
            .AsNoTracking()
            .Where(auditEvent => auditEvent.IntakeDocumentId == id)
            .OrderBy(auditEvent => auditEvent.CreatedUtc)
            .Select(auditEvent => new AuditEventDetailResponse(
                auditEvent.EventType,
                auditEvent.Message,
                auditEvent.CreatedUtc))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(new ReviewDetailResponse(
            document.Id,
            document.DisplayName,
            document.SampleDocumentId,
            document.Scenario,
            document.Summary,
            document.DocumentText,
            document.Status.ToString(),
            reviewState,
            processingResult.DocumentType,
            processingResult.OverallConfidence,
            processingResult.Rationale,
            processingResult.SuggestedRouting,
            extractedFields,
            validationFlags,
            auditEvents,
            document.CreatedUtc,
            document.UpdatedUtc));
    }

    private sealed record ValidationFlagSummary(
        int Count,
        ValidationFlagSeverity HighestSeverity);
}

public sealed record ReviewQueueItemResponse(
    Guid IntakeDocumentId,
    string DisplayName,
    string WorkflowStatus,
    string DocumentType,
    decimal OverallConfidence,
    int ValidationFlagCount,
    string? HighestSeverity,
    string? SampleDocumentId,
    string? Scenario,
    DateTime UpdatedUtc);

public sealed record ReviewDetailResponse(
    Guid IntakeDocumentId,
    string DisplayName,
    string? SampleDocumentId,
    string? Scenario,
    string? Summary,
    string? DocumentText,
    string WorkflowStatus,
    ReviewStateDetailResponse? ReviewState,
    string DocumentType,
    decimal OverallConfidence,
    string Rationale,
    string SuggestedRouting,
    ExtractedFieldDetailResponse[] ExtractedFields,
    ValidationFlagDetailResponse[] ValidationFlags,
    AuditEventDetailResponse[] AuditEvents,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record ReviewStateDetailResponse(
    bool RequiresHumanReview,
    string? Decision,
    string? DecidedBy,
    DateTime? DecidedUtc,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record ExtractedFieldDetailResponse(
    string Name,
    string Value,
    decimal Confidence,
    string? ReviewedValue,
    string? ReviewedBy,
    DateTime? ReviewedUtc);

public sealed record ValidationFlagDetailResponse(
    string FlagType,
    string Severity,
    string? FieldName,
    string Message,
    DateTime CreatedUtc);

public sealed record AuditEventDetailResponse(
    string EventType,
    string Message,
    DateTime CreatedUtc);
