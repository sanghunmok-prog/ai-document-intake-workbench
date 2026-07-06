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

namespace AiDocumentIntakeWorkbench.Api.Tests.Workflow;

public sealed class WorkflowReliabilityTests
{
    [Fact]
    public async Task IntakeProcessingReviewWorkflow_WritesExpectedAuditEventsForSameDocument()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.MissingLowConfidence);
        var reviewService = new ReviewWorkflowService(context);

        await reviewService.UpdateFieldsAsync(
            document.Id,
            [new ReviewerFieldUpdate("ServiceNeeded", "Replace damaged lobby access badge reader")]);
        await reviewService.RecordDecisionAsync(document.Id, "NeedsCorrection");

        var auditEvents = await context.AuditEvents
            .Where(auditEvent => auditEvent.IntakeDocumentId == document.Id)
            .OrderBy(auditEvent => auditEvent.CreatedUtc)
            .ToArrayAsync();

        Assert.All(auditEvents, auditEvent => Assert.Equal(document.Id, auditEvent.IntakeDocumentId));
        Assert.Contains(auditEvents, auditEvent => auditEvent.EventType == "SampleDocumentSelected");
        Assert.Contains(auditEvents, auditEvent => auditEvent.EventType == "AiProcessingCompleted");
        Assert.Contains(auditEvents, auditEvent => auditEvent.EventType == "ValidationFlagsCreated");
        Assert.Contains(auditEvents, auditEvent => auditEvent.EventType == "ReviewerFieldEdited");
        Assert.Contains(auditEvents, auditEvent => auditEvent.EventType == "ReviewerDecisionRecorded");
        Assert.Equal(2, auditEvents.Count(auditEvent => auditEvent.EventType == "WorkflowStatusChanged"));
    }

    [Fact]
    public async Task ReviewDetail_AuditHistory_IsSortedByTimestamp()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var reviewService = new ReviewWorkflowService(context);
        await reviewService.UpdateFieldsAsync(
            document.Id,
            [new ReviewerFieldUpdate("InvoiceNumber", "INV-10482-CORRECTED")]);
        await reviewService.RecordDecisionAsync(document.Id, "Approved");

        var detail = await GetReviewDetailAsync(context, document.Id);

        Assert.True(detail.AuditEvents.Length >= 4);
        Assert.Equal(
            detail.AuditEvents.OrderBy(auditEvent => auditEvent.CreatedUtc).ToArray(),
            detail.AuditEvents);
    }

    [Theory]
    [InlineData(SampleDocumentIds.CleanHighConfidence, 0)]
    [InlineData(SampleDocumentIds.MissingLowConfidence, 1)]
    [InlineData(SampleDocumentIds.ConflictingInconsistent, 1)]
    public async Task ProcessingKnownSamples_RoutesToReviewWithoutFinalDecision(
        string sampleDocumentId,
        int minimumValidationFlags)
    {
        using var context = CreateContext();

        var document = await CreateProcessedDocumentAsync(context, sampleDocumentId);

        var persistedDocument = await context.IntakeDocuments.SingleAsync(item => item.Id == document.Id);
        var reviewState = await context.ReviewStates.SingleAsync(state => state.IntakeDocumentId == document.Id);
        var validationFlagCount = await context.ValidationFlags.CountAsync(flag => flag.IntakeDocumentId == document.Id);

        Assert.Equal(WorkflowStatus.AwaitingReview, persistedDocument.Status);
        Assert.Null(reviewState.Decision);
        Assert.True(reviewState.RequiresHumanReview);
        Assert.True(validationFlagCount >= minimumValidationFlags);
    }

    [Fact]
    public async Task InvalidSampleDocumentId_ReturnsNotFoundAndDoesNotWriteAudit()
    {
        using var context = CreateContext();
        var service = new IntakeDocumentService(context, new InMemorySampleDocumentCatalog());

        var result = await IntakeDocumentEndpoints.CreateFromSampleAsync(
            new CreateIntakeDocumentFromSampleRequest("unknown-sample"),
            service,
            CancellationToken.None);

        AssertStatusCode(result, StatusCodes.Status404NotFound);
        Assert.Empty(context.IntakeDocuments);
        Assert.Empty(context.AuditEvents);
    }

    [Fact]
    public async Task AiProcessingEndpoint_UnknownDocument_ReturnsNotFoundWithoutAudit()
    {
        using var context = CreateContext();
        var service = CreateAiProcessingService(context);

        var result = await AiProcessingEndpoints.ProcessAsync(Guid.NewGuid(), service, CancellationToken.None);

        AssertStatusCode(result, StatusCodes.Status404NotFound);
        Assert.Empty(context.AuditEvents);
    }

    [Fact]
    public async Task AiProcessingEndpoint_AlreadyProcessedDocument_ReturnsConflictWithoutNewAuditEvents()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var auditCountAfterProcessing = await context.AuditEvents.CountAsync();
        var service = CreateAiProcessingService(context);

        var result = await AiProcessingEndpoints.ProcessAsync(document.Id, service, CancellationToken.None);

        AssertStatusCode(result, StatusCodes.Status409Conflict);
        Assert.Equal(auditCountAfterProcessing, await context.AuditEvents.CountAsync());
    }

    [Fact]
    public async Task AiProcessingEndpoint_FinalizedDocument_ReturnsConflictWithoutNewAuditEvents()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var reviewService = new ReviewWorkflowService(context);
        await reviewService.RecordDecisionAsync(document.Id, "Approved");
        var auditCountAfterDecision = await context.AuditEvents.CountAsync();
        var service = CreateAiProcessingService(context);

        var result = await AiProcessingEndpoints.ProcessAsync(document.Id, service, CancellationToken.None);

        AssertStatusCode(result, StatusCodes.Status409Conflict);
        Assert.Equal(auditCountAfterDecision, await context.AuditEvents.CountAsync());
        Assert.Equal(WorkflowStatus.Approved, (await context.IntakeDocuments.SingleAsync()).Status);
    }

    [Fact]
    public async Task FieldEditWithMixedValidAndUnknownFields_DoesNotPartiallyMutate()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = new ReviewWorkflowService(context);

        var result = await service.UpdateFieldsAsync(
            document.Id,
            [
                new ReviewerFieldUpdate("InvoiceNumber", "INV-10482-CORRECTED"),
                new ReviewerFieldUpdate("UnknownField", "value")
            ]);

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewWorkflowError.UnknownField, result.Error);
        Assert.All(await context.ExtractedDocumentFields.ToArrayAsync(), field => Assert.Null(field.ReviewedValue));
        Assert.DoesNotContain(
            await context.AuditEvents.ToArrayAsync(),
            auditEvent => auditEvent.EventType == "ReviewerFieldEdited");
    }

    [Fact]
    public async Task DuplicateFieldUpdates_AreRejectedWithoutAuditEvents()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var auditCountAfterProcessing = await context.AuditEvents.CountAsync();
        var service = new ReviewWorkflowService(context);

        var result = await service.UpdateFieldsAsync(
            document.Id,
            [
                new ReviewerFieldUpdate("InvoiceNumber", "INV-10482-CORRECTED"),
                new ReviewerFieldUpdate("invoicenumber", "INV-10482-SECOND")
            ]);

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewWorkflowError.InvalidFieldUpdate, result.Error);
        Assert.Equal(auditCountAfterProcessing, await context.AuditEvents.CountAsync());
    }

    [Fact]
    public async Task NumericReviewerDecision_IsRejectedAsInvalidInput()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var auditCountAfterProcessing = await context.AuditEvents.CountAsync();
        var service = new ReviewWorkflowService(context);

        var result = await service.RecordDecisionAsync(document.Id, "0");

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewWorkflowError.InvalidDecision, result.Error);
        Assert.Equal(WorkflowStatus.AwaitingReview, (await context.IntakeDocuments.SingleAsync()).Status);
        Assert.Equal(auditCountAfterProcessing, await context.AuditEvents.CountAsync());
    }

    [Fact]
    public async Task ReviewWorkflowEndpoints_MapValidationNotFoundAndConflictResponses()
    {
        using var context = CreateContext();
        var document = await CreateIntakeDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = new ReviewWorkflowService(context);

        var missingFieldUpdate = await ReviewWorkflowEndpoints.UpdateFieldsAsync(
            Guid.NewGuid(),
            new ReviewFieldUpdatesRequest([new ReviewFieldUpdateRequest("InvoiceNumber", "INV-10482")]),
            service,
            CancellationToken.None);
        var invalidDecision = await ReviewWorkflowEndpoints.RecordDecisionAsync(
            document.Id,
            new ReviewerDecisionRequest("Maybe"),
            service,
            CancellationToken.None);
        var notReviewableDecision = await ReviewWorkflowEndpoints.RecordDecisionAsync(
            document.Id,
            new ReviewerDecisionRequest("Approved"),
            service,
            CancellationToken.None);

        AssertStatusCode(missingFieldUpdate, StatusCodes.Status404NotFound);
        AssertStatusCode(invalidDecision, StatusCodes.Status400BadRequest);
        AssertStatusCode(notReviewableDecision, StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task DuplicateFinalDecisionEndpoint_ReturnsConflictWithoutDuplicateAuditEvents()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = new ReviewWorkflowService(context);
        await ReviewWorkflowEndpoints.RecordDecisionAsync(
            document.Id,
            new ReviewerDecisionRequest("Approved"),
            service,
            CancellationToken.None);
        var auditCountAfterApproval = await context.AuditEvents.CountAsync();

        var result = await ReviewWorkflowEndpoints.RecordDecisionAsync(
            document.Id,
            new ReviewerDecisionRequest("Rejected"),
            service,
            CancellationToken.None);

        AssertStatusCode(result, StatusCodes.Status409Conflict);
        Assert.Equal(auditCountAfterApproval, await context.AuditEvents.CountAsync());
        Assert.Single(await context.AuditEvents
            .Where(auditEvent => auditEvent.EventType == "ReviewerDecisionRecorded")
            .ToArrayAsync());
    }

    private static async Task<IntakeDocument> CreateProcessedDocumentAsync(
        WorkbenchDbContext context,
        string sampleDocumentId)
    {
        var document = await CreateIntakeDocumentAsync(context, sampleDocumentId);
        var processingService = CreateAiProcessingService(context);
        var result = await processingService.ProcessAsync(document.Id);

        Assert.True(result.Succeeded);

        return document;
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

    private static async Task<ReviewDetailResponse> GetReviewDetailAsync(
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

    private static AiProcessingService CreateAiProcessingService(WorkbenchDbContext context)
    {
        return new AiProcessingService(context, new DeterministicMockDocumentAiProcessor());
    }

    private static void AssertStatusCode(IResult result, int expectedStatusCode)
    {
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(expectedStatusCode, statusResult.StatusCode);
    }

    private static WorkbenchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WorkbenchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WorkbenchDbContext(options);
    }
}
