using AiDocumentIntakeWorkbench.Api.Ai;
using AiDocumentIntakeWorkbench.Api.AiProcessing;
using AiDocumentIntakeWorkbench.Api.Api;
using AiDocumentIntakeWorkbench.Api.Data;
using AiDocumentIntakeWorkbench.Api.Domain;
using AiDocumentIntakeWorkbench.Api.Intake;
using AiDocumentIntakeWorkbench.Api.SampleDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AiDocumentIntakeWorkbench.Api.Tests.AiProcessing;

public sealed class AiProcessingServiceTests
{
    [Fact]
    public async Task ProcessAsync_CleanSample_PersistsResultAndFieldsWithoutValidationFlags()
    {
        using var context = CreateContext();
        var document = await CreateIntakeDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = CreateService(context);

        var result = await service.ProcessAsync(document.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Summary);
        Assert.Equal(document.Id, result.Summary.IntakeDocumentId);
        Assert.Equal(WorkflowStatus.AwaitingReview.ToString(), result.Summary.WorkflowStatus);
        Assert.Equal("VendorInvoice", result.Summary.DocumentType);
        Assert.True(result.Summary.OverallConfidence >= 0.90m);
        Assert.Empty(result.Summary.ValidationFlags);

        var persistedResult = await context.DocumentProcessingResults.SingleAsync();
        Assert.Equal(document.Id, persistedResult.IntakeDocumentId);
        Assert.Equal("VendorInvoice", persistedResult.DocumentType);
        Assert.NotEmpty(await context.ExtractedDocumentFields.ToArrayAsync());
        Assert.Empty(context.ValidationFlags);
    }

    [Fact]
    public async Task ProcessAsync_MissingSample_CreatesMissingAndLowConfidenceFlags()
    {
        using var context = CreateContext();
        var document = await CreateIntakeDocumentAsync(context, SampleDocumentIds.MissingLowConfidence);
        var service = CreateService(context);

        var result = await service.ProcessAsync(document.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Summary);
        Assert.Equal(WorkflowStatus.AwaitingReview.ToString(), result.Summary.WorkflowStatus);
        Assert.Contains(result.Summary.ValidationFlags, flag => flag.FlagType == ValidationFlagType.MissingRequiredField.ToString());
        Assert.Contains(result.Summary.ValidationFlags, flag => flag.FlagType == ValidationFlagType.LowConfidence.ToString());

        var flags = await context.ValidationFlags.ToArrayAsync();
        Assert.Contains(flags, flag => flag.FlagType == ValidationFlagType.MissingRequiredField && flag.FieldName == "AccountReference");
        Assert.Contains(flags, flag => flag.FlagType == ValidationFlagType.LowConfidence);
    }

    [Fact]
    public async Task ProcessAsync_ConflictingSample_CreatesInconsistentDataFlag()
    {
        using var context = CreateContext();
        var document = await CreateIntakeDocumentAsync(context, SampleDocumentIds.ConflictingInconsistent);
        var service = CreateService(context);

        var result = await service.ProcessAsync(document.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Summary);
        Assert.Equal("PurchaseOrder", result.Summary.DocumentType);
        Assert.Contains(result.Summary.ExtractedFields, field => field.Name == "StatedOrderTotal" && field.Value == "$1,900.00");
        Assert.Contains(result.Summary.ValidationFlags, flag => flag.FlagType == ValidationFlagType.InconsistentData.ToString());

        var flags = await context.ValidationFlags.ToArrayAsync();
        Assert.Contains(flags, flag => flag.FlagType == ValidationFlagType.InconsistentData);
    }

    [Fact]
    public async Task ProcessAsync_RoutesToReviewStateAndWritesAuditEvents()
    {
        using var context = CreateContext();
        var document = await CreateIntakeDocumentAsync(context, SampleDocumentIds.MissingLowConfidence);
        var service = CreateService(context);

        var result = await service.ProcessAsync(document.Id);

        Assert.True(result.Succeeded);

        var persistedDocument = await context.IntakeDocuments.SingleAsync(item => item.Id == document.Id);
        Assert.Equal(WorkflowStatus.AwaitingReview, persistedDocument.Status);
        Assert.True(await context.ReviewStates.AnyAsync(reviewState => reviewState.IntakeDocumentId == document.Id));

        var auditTypes = await context.AuditEvents
            .Where(auditEvent => auditEvent.IntakeDocumentId == document.Id)
            .Select(auditEvent => auditEvent.EventType)
            .ToArrayAsync();

        Assert.Contains("AiProcessingCompleted", auditTypes);
        Assert.Contains("ValidationFlagsCreated", auditTypes);
        Assert.Contains("WorkflowStatusChanged", auditTypes);
    }

    [Fact]
    public async Task ProcessAsync_InvalidDocumentId_ReturnsControlledNotFoundError()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ProcessAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(ProcessIntakeDocumentError.DocumentNotFound, result.Error);
    }

    [Fact]
    public async Task ProcessAsync_RepeatedProcessing_ReturnsControlledConflict()
    {
        using var context = CreateContext();
        var document = await CreateIntakeDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = CreateService(context);

        var first = await service.ProcessAsync(document.Id);
        var second = await service.ProcessAsync(document.Id);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(ProcessIntakeDocumentError.AlreadyProcessed, second.Error);
        Assert.Equal(1, await context.DocumentProcessingResults.CountAsync());
    }

    [Fact]
    public async Task ProcessEndpoint_RepeatedProcessing_ReturnsConflict()
    {
        using var context = CreateContext();
        var document = await CreateIntakeDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var service = CreateService(context);

        var first = await AiProcessingEndpoints.ProcessAsync(document.Id, service, CancellationToken.None);
        var second = await AiProcessingEndpoints.ProcessAsync(document.Id, service, CancellationToken.None);

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(first);
        var conflictResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(second);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }

    private static AiProcessingService CreateService(WorkbenchDbContext context)
    {
        return new AiProcessingService(context, new DeterministicMockDocumentAiProcessor());
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
