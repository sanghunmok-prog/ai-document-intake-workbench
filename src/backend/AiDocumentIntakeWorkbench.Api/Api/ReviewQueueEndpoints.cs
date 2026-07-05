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
