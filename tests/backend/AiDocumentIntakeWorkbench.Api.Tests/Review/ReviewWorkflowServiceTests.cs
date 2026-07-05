using AiDocumentIntakeWorkbench.Api.Ai;
using AiDocumentIntakeWorkbench.Api.AiProcessing;
using AiDocumentIntakeWorkbench.Api.Data;
using AiDocumentIntakeWorkbench.Api.Domain;
using AiDocumentIntakeWorkbench.Api.Intake;
using AiDocumentIntakeWorkbench.Api.Review;
using AiDocumentIntakeWorkbench.Api.SampleDocuments;
using Microsoft.EntityFrameworkCore;

namespace AiDocumentIntakeWorkbench.Api.Tests.Review;

public sealed class ReviewWorkflowServiceTests
{
    [Fact]
    public async Task UpdateFieldsAsync_ReviewableDocument_PersistsReviewedValues()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = new ReviewWorkflowService(context);

        var result = await service.UpdateFieldsAsync(
            document.Id,
            [
                new ReviewerFieldUpdate("InvoiceNumber", "INV-10482-CORRECTED"),
                new ReviewerFieldUpdate("TotalDue", "$4,218.40 verified")
            ]);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Response?.EditedFieldCount);

        var invoiceNumber = await context.ExtractedDocumentFields
            .SingleAsync(field => field.Name == "InvoiceNumber");
        var totalDue = await context.ExtractedDocumentFields
            .SingleAsync(field => field.Name == "TotalDue");

        Assert.Equal("INV-10482", invoiceNumber.Value);
        Assert.Equal("INV-10482-CORRECTED", invoiceNumber.ReviewedValue);
        Assert.Equal("Reviewer", invoiceNumber.ReviewedBy);
        Assert.NotNull(invoiceNumber.ReviewedUtc);
        Assert.Equal("$4,218.40 verified", totalDue.ReviewedValue);
    }

    [Fact]
    public async Task UpdateFieldsAsync_ReviewableDocument_WritesAuditEvents()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var originalAuditCount = await context.AuditEvents.CountAsync();
        var service = new ReviewWorkflowService(context);

        var result = await service.UpdateFieldsAsync(
            document.Id,
            [new ReviewerFieldUpdate("InvoiceNumber", "INV-10482-CORRECTED")]);

        Assert.True(result.Succeeded);
        Assert.Equal(originalAuditCount + 1, await context.AuditEvents.CountAsync());
        Assert.Contains(
            await context.AuditEvents.ToArrayAsync(),
            auditEvent => auditEvent.EventType == "ReviewerFieldEdited"
                && auditEvent.Message.Contains("InvoiceNumber"));
    }

    [Theory]
    [InlineData("Approved", WorkflowStatus.Approved, ReviewerDecision.Approved)]
    [InlineData("Rejected", WorkflowStatus.Rejected, ReviewerDecision.Rejected)]
    [InlineData("NeedsCorrection", WorkflowStatus.NeedsCorrection, ReviewerDecision.NeedsCorrection)]
    public async Task RecordDecisionAsync_ReviewableDocument_PersistsDecisionAndUpdatesStatus(
        string decision,
        WorkflowStatus expectedStatus,
        ReviewerDecision expectedDecision)
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = new ReviewWorkflowService(context);

        var result = await service.RecordDecisionAsync(document.Id, decision);

        Assert.True(result.Succeeded);
        Assert.Equal(expectedStatus.ToString(), result.Response?.WorkflowStatus);
        Assert.Equal(expectedDecision.ToString(), result.Response?.Decision);

        var persistedDocument = await context.IntakeDocuments.SingleAsync(item => item.Id == document.Id);
        var reviewState = await context.ReviewStates.SingleAsync(state => state.IntakeDocumentId == document.Id);

        Assert.Equal(expectedStatus, persistedDocument.Status);
        Assert.Equal(expectedDecision, reviewState.Decision);
        Assert.Equal("Reviewer", reviewState.DecidedBy);
        Assert.NotNull(reviewState.DecidedUtc);
        Assert.False(reviewState.RequiresHumanReview);
    }

    [Fact]
    public async Task RecordDecisionAsync_ReviewableDocument_WritesDecisionAndStatusAuditEvents()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = new ReviewWorkflowService(context);

        var result = await service.RecordDecisionAsync(document.Id, "Approved");

        Assert.True(result.Succeeded);

        var auditTypes = await context.AuditEvents
            .Where(auditEvent => auditEvent.IntakeDocumentId == document.Id)
            .Select(auditEvent => auditEvent.EventType)
            .ToArrayAsync();

        Assert.Contains("ReviewerDecisionRecorded", auditTypes);
        Assert.Contains("WorkflowStatusChanged", auditTypes);
    }

    [Fact]
    public async Task UpdateFieldsAsync_InvalidDocumentId_ReturnsNotFound()
    {
        using var context = CreateContext();
        var service = new ReviewWorkflowService(context);

        var result = await service.UpdateFieldsAsync(
            Guid.NewGuid(),
            [new ReviewerFieldUpdate("InvoiceNumber", "INV-10482-CORRECTED")]);

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewWorkflowError.DocumentNotFound, result.Error);
    }

    [Fact]
    public async Task UpdateFieldsAsync_UnknownField_ReturnsValidationFailure()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = new ReviewWorkflowService(context);

        var result = await service.UpdateFieldsAsync(
            document.Id,
            [new ReviewerFieldUpdate("UnknownField", "value")]);

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewWorkflowError.UnknownField, result.Error);
        Assert.Empty(await context.AuditEvents
            .Where(auditEvent => auditEvent.EventType == "ReviewerFieldEdited")
            .ToArrayAsync());
    }

    [Fact]
    public async Task UpdateFieldsAsync_EmptyReviewedValue_ReturnsValidationFailure()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = new ReviewWorkflowService(context);

        var result = await service.UpdateFieldsAsync(
            document.Id,
            [new ReviewerFieldUpdate("InvoiceNumber", " ")]);

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewWorkflowError.InvalidFieldUpdate, result.Error);
    }

    [Fact]
    public async Task RecordDecisionAsync_InvalidDecision_ReturnsValidationFailure()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = new ReviewWorkflowService(context);

        var result = await service.RecordDecisionAsync(document.Id, "Maybe");

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewWorkflowError.InvalidDecision, result.Error);
    }

    [Fact]
    public async Task RecordDecisionAsync_BeforeReviewableState_ReturnsConflict()
    {
        using var context = CreateContext();
        var document = await CreateIntakeDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = new ReviewWorkflowService(context);

        var result = await service.RecordDecisionAsync(document.Id, "Approved");

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewWorkflowError.NotReviewable, result.Error);
        Assert.Equal(WorkflowStatus.Received, (await context.IntakeDocuments.SingleAsync()).Status);
    }

    [Fact]
    public async Task RecordDecisionAsync_DuplicateFinalDecision_ReturnsConflictWithoutNewAuditEvents()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = new ReviewWorkflowService(context);

        var first = await service.RecordDecisionAsync(document.Id, "Approved");
        var auditCountAfterFirstDecision = await context.AuditEvents.CountAsync();
        var second = await service.RecordDecisionAsync(document.Id, "Rejected");

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(ReviewWorkflowError.AlreadyFinalized, second.Error);
        Assert.Equal(auditCountAfterFirstDecision, await context.AuditEvents.CountAsync());
    }

    [Fact]
    public async Task UpdateFieldsAsync_AfterFinalDecision_ReturnsConflict()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = new ReviewWorkflowService(context);
        await service.RecordDecisionAsync(document.Id, "Approved");

        var result = await service.UpdateFieldsAsync(
            document.Id,
            [new ReviewerFieldUpdate("InvoiceNumber", "INV-10482-CORRECTED")]);

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewWorkflowError.AlreadyFinalized, result.Error);
    }

    [Fact]
    public async Task ProcessAsync_DoesNotMakeFinalReviewerDecision()
    {
        using var context = CreateContext();
        var document = await CreateProcessedDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);

        var persistedDocument = await context.IntakeDocuments.SingleAsync(item => item.Id == document.Id);
        var reviewState = await context.ReviewStates.SingleAsync(state => state.IntakeDocumentId == document.Id);

        Assert.Equal(WorkflowStatus.AwaitingReview, persistedDocument.Status);
        Assert.Null(reviewState.Decision);
        Assert.True(reviewState.RequiresHumanReview);
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
