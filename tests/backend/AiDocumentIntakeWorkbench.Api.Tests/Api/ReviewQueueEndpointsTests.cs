using AiDocumentIntakeWorkbench.Api.Ai;
using AiDocumentIntakeWorkbench.Api.AiProcessing;
using AiDocumentIntakeWorkbench.Api.Api;
using AiDocumentIntakeWorkbench.Api.Data;
using AiDocumentIntakeWorkbench.Api.Domain;
using AiDocumentIntakeWorkbench.Api.Intake;
using AiDocumentIntakeWorkbench.Api.SampleDocuments;
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
