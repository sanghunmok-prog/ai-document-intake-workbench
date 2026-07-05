using AiDocumentIntakeWorkbench.Api.Ai;
using AiDocumentIntakeWorkbench.Api.AiProcessing;
using AiDocumentIntakeWorkbench.Api.Api;
using AiDocumentIntakeWorkbench.Api.Data;
using AiDocumentIntakeWorkbench.Api.Domain;
using AiDocumentIntakeWorkbench.Api.Intake;
using AiDocumentIntakeWorkbench.Api.Review;
using AiDocumentIntakeWorkbench.Api.SampleDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AiDocumentIntakeWorkbench.Api.Tests.Api;

public sealed class ReviewQueueEndpointsTests
{
    [Fact]
    public async Task ListAsync_EmptyQueue_ReturnsEmptyList()
    {
        using var context = CreateContext();

        var result = await ListQueueAsync(context);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_ProcessedCleanSample_ReturnsSummaryWithoutFlags()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);

        var result = await ListQueueAsync(context);

        var item = Assert.Single(result);
        Assert.Equal(document.Id, item.IntakeDocumentId);
        Assert.Equal("Vendor invoice with complete remittance details", item.DisplayName);
        Assert.Equal(WorkflowStatus.AwaitingReview.ToString(), item.WorkflowStatus);
        Assert.Equal("VendorInvoice", item.DocumentType);
        Assert.True(item.OverallConfidence >= 0.90m);
        Assert.Equal(0, item.ValidationFlagCount);
        Assert.Null(item.HighestSeverity);
        Assert.Equal(SampleDocumentIds.CleanHighConfidence, item.SampleDocumentId);
        Assert.Equal("Complete invoice scenario", item.Scenario);
    }

    [Fact]
    public async Task ListAsync_FlaggedLowConfidenceSample_ReturnsFlagSummary()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.MissingLowConfidence);

        var result = await ListQueueAsync(context);

        var item = Assert.Single(result);
        Assert.Equal(document.Id, item.IntakeDocumentId);
        Assert.True(item.ValidationFlagCount > 0);
        Assert.Equal(ValidationFlagSeverity.Error.ToString(), item.HighestSeverity);
        Assert.True(item.OverallConfidence < 0.75m);
    }

    [Fact]
    public async Task ListAsync_UnprocessedIntakeDocument_IsExcluded()
    {
        using var context = CreateContext();
        await CreateIntakeDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);

        var result = await ListQueueAsync(context);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_ResponseShapeIsSummaryOnlyAndDoesNotMutateState()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.ConflictingInconsistent);
        var originalStatus = document.Status;
        var originalAuditEventCount = await context.AuditEvents.CountAsync();
        var originalFlagCount = await context.ValidationFlags.CountAsync();

        var first = await ListQueueAsync(context);
        var second = await ListQueueAsync(context);

        Assert.Equal(first.Length, second.Length);
        Assert.Equal(originalStatus, (await context.IntakeDocuments.SingleAsync()).Status);
        Assert.Equal(originalAuditEventCount, await context.AuditEvents.CountAsync());
        Assert.Equal(originalFlagCount, await context.ValidationFlags.CountAsync());

        var responseProperties = typeof(ReviewQueueItemResponse)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(
            [
                nameof(ReviewQueueItemResponse.IntakeDocumentId),
                nameof(ReviewQueueItemResponse.DisplayName),
                nameof(ReviewQueueItemResponse.WorkflowStatus),
                nameof(ReviewQueueItemResponse.DocumentType),
                nameof(ReviewQueueItemResponse.OverallConfidence),
                nameof(ReviewQueueItemResponse.ValidationFlagCount),
                nameof(ReviewQueueItemResponse.HighestSeverity),
                nameof(ReviewQueueItemResponse.SampleDocumentId),
                nameof(ReviewQueueItemResponse.Scenario),
                nameof(ReviewQueueItemResponse.UpdatedUtc)
            ],
            responseProperties);
    }

    [Fact]
    public async Task GetDetailAsync_ProcessedSample_ReturnsDocumentContextAndAiSummary()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);

        var detail = await GetDetailValueAsync(context, document.Id);

        Assert.Equal(document.Id, detail.IntakeDocumentId);
        Assert.Equal("Vendor invoice with complete remittance details", detail.DisplayName);
        Assert.Equal(SampleDocumentIds.CleanHighConfidence, detail.SampleDocumentId);
        Assert.Equal("Complete invoice scenario", detail.Scenario);
        Assert.Contains("clear supplier", detail.Summary ?? string.Empty);
        Assert.Contains("Invoice INV-10482", detail.DocumentText ?? string.Empty);
        Assert.Equal(WorkflowStatus.AwaitingReview.ToString(), detail.WorkflowStatus);
        Assert.NotNull(detail.ReviewState);
        Assert.True(detail.ReviewState?.RequiresHumanReview == true);
        Assert.Equal("VendorInvoice", detail.DocumentType);
        Assert.True(detail.OverallConfidence >= 0.90m);
        Assert.False(string.IsNullOrWhiteSpace(detail.Rationale));
        Assert.False(string.IsNullOrWhiteSpace(detail.SuggestedRouting));
    }

    [Fact]
    public async Task GetDetailAsync_FlaggedSample_ReturnsFieldsFlagsAndAuditEvents()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.MissingLowConfidence);

        var detail = await GetDetailValueAsync(context, document.Id);

        Assert.Contains(detail.ExtractedFields, field => field.Name == "ServiceNeeded");
        Assert.All(detail.ExtractedFields, field => Assert.InRange(field.Confidence, 0m, 1m));

        Assert.Contains(
            detail.ValidationFlags,
            flag => flag.FlagType == ValidationFlagType.MissingRequiredField.ToString()
                && flag.Severity == ValidationFlagSeverity.Error.ToString()
                && flag.FieldName == "AccountReference");
        Assert.Contains(
            detail.ValidationFlags,
            flag => flag.FlagType == ValidationFlagType.LowConfidence.ToString());

        Assert.Contains(detail.AuditEvents, auditEvent => auditEvent.EventType == "SampleDocumentSelected");
        Assert.Contains(detail.AuditEvents, auditEvent => auditEvent.EventType == "AiProcessingCompleted");
        Assert.Contains(detail.AuditEvents, auditEvent => auditEvent.EventType == "ValidationFlagsCreated");
        Assert.Contains(detail.AuditEvents, auditEvent => auditEvent.EventType == "WorkflowStatusChanged");
    }

    [Fact]
    public async Task GetDetailAsync_MissingDocument_ReturnsNotFound()
    {
        using var context = CreateContext();

        var result = await ReviewQueueEndpoints.GetDetailAsync(
            Guid.NewGuid(),
            context,
            CancellationToken.None);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetDetailAsync_ReadOnlyRequest_DoesNotMutateState()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.ConflictingInconsistent);
        var originalStatus = document.Status;
        var originalAuditEventCount = await context.AuditEvents.CountAsync();
        var originalFieldCount = await context.ExtractedDocumentFields.CountAsync();
        var originalFlagCount = await context.ValidationFlags.CountAsync();

        await GetDetailValueAsync(context, document.Id);
        await GetDetailValueAsync(context, document.Id);

        Assert.Equal(originalStatus, (await context.IntakeDocuments.SingleAsync()).Status);
        Assert.Equal(originalAuditEventCount, await context.AuditEvents.CountAsync());
        Assert.Equal(originalFieldCount, await context.ExtractedDocumentFields.CountAsync());
        Assert.Equal(originalFlagCount, await context.ValidationFlags.CountAsync());
    }

    [Fact]
    public async Task GetDetailAsync_FinalizedReview_ReturnsReviewedValuesAndDecision()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var reviewWorkflowService = new ReviewWorkflowService(context);
        await reviewWorkflowService.UpdateFieldsAsync(
            document.Id,
            [new ReviewerFieldUpdate("InvoiceNumber", "INV-10482-CORRECTED")]);
        await reviewWorkflowService.RecordDecisionAsync(document.Id, "Approved");

        var detail = await GetDetailValueAsync(context, document.Id);

        Assert.Equal(WorkflowStatus.Approved.ToString(), detail.WorkflowStatus);
        Assert.Equal(ReviewerDecision.Approved.ToString(), detail.ReviewState?.Decision);
        Assert.Contains(
            detail.ExtractedFields,
            field => field.Name == "InvoiceNumber"
                && field.Value == "INV-10482"
                && field.ReviewedValue == "INV-10482-CORRECTED");
        Assert.Contains(detail.AuditEvents, auditEvent => auditEvent.EventType == "ReviewerFieldEdited");
        Assert.Contains(detail.AuditEvents, auditEvent => auditEvent.EventType == "ReviewerDecisionRecorded");
    }

    private static async Task<IntakeDocument> CreateProcessedDocumentAsync(
        WorkbenchDbContext context,
        string sampleDocumentId)
    {
        var document = await CreateIntakeDocumentAsync(context, sampleDocumentId);
        var processingService = new AiProcessingService(context, new DeterministicMockDocumentAiProcessor());
        var result = await processingService.ProcessAsync(document.Id);

        Assert.True(result.Succeeded);

        return document;
    }

    private static async Task<ReviewQueueItemResponse[]> ListQueueAsync(WorkbenchDbContext context)
    {
        var result = await ReviewQueueEndpoints.ListAsync(context, CancellationToken.None);
        return result.Value ?? throw new InvalidOperationException("Review queue endpoint returned no value.");
    }

    private static async Task<ReviewDetailResponse> GetDetailValueAsync(
        WorkbenchDbContext context,
        Guid intakeDocumentId)
    {
        var result = await ReviewQueueEndpoints.GetDetailAsync(
            intakeDocumentId,
            context,
            CancellationToken.None);
        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);

        return Assert.IsType<ReviewDetailResponse>(valueResult.Value);
    }

    private static async Task<IntakeDocument> CreateIntakeDocumentAsync(
        WorkbenchDbContext context,
        string sampleDocumentId)
    {
        var intakeService = new IntakeDocumentService(context, new InMemorySampleDocumentCatalog());
        var result = await intakeService.CreateFromSampleAsync(sampleDocumentId);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.IntakeDocument);

        return result.IntakeDocument;
    }

    private static WorkbenchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WorkbenchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WorkbenchDbContext(options);
    }
}
