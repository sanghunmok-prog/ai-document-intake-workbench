using AiDocumentIntakeWorkbench.Api.Api;
using AiDocumentIntakeWorkbench.Api.Data;
using AiDocumentIntakeWorkbench.Api.Domain;
using AiDocumentIntakeWorkbench.Api.Intake;
using AiDocumentIntakeWorkbench.Api.SampleDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AiDocumentIntakeWorkbench.Api.Tests.Intake;

public sealed class IntakeDocumentServiceTests
{
    [Fact]
    public async Task CreateFromSampleAsync_PersistsIntakeDocumentFromValidSample()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateFromSampleAsync("clean-high-confidence");

        Assert.True(result.Succeeded);

        var document = await context.IntakeDocuments.SingleAsync();
        Assert.Equal(result.IntakeDocument?.Id, document.Id);
        Assert.Equal("Vendor invoice with complete remittance details", document.DisplayName);
        Assert.Equal("clean-high-confidence", document.SampleDocumentId);
        Assert.Equal("Complete invoice scenario", document.Scenario);
        Assert.False(string.IsNullOrWhiteSpace(document.DocumentText));
    }

    [Fact]
    public async Task CreateFromSampleAsync_SetsInitialWorkflowStatus()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateFromSampleAsync("missing-low-confidence");

        Assert.True(result.Succeeded);
        Assert.Equal(WorkflowStatus.Received, result.IntakeDocument?.Status);
    }

    [Fact]
    public async Task CreateFromSampleAsync_CreatesAuditEvent()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateFromSampleAsync("conflicting-inconsistent");

        Assert.True(result.Succeeded);

        var auditEvent = await context.AuditEvents.SingleAsync();
        Assert.Equal(result.IntakeDocument?.Id, auditEvent.IntakeDocumentId);
        Assert.Equal("SampleDocumentSelected", auditEvent.EventType);
        Assert.Contains("conflicting-inconsistent", auditEvent.Message);
    }

    [Fact]
    public async Task CreateFromSampleAsync_ReturnsFailureForUnknownSample()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateFromSampleAsync("unknown-sample");

        Assert.False(result.Succeeded);
        Assert.Equal(CreateIntakeDocumentError.SampleDocumentNotFound, result.Error);
        Assert.Empty(context.IntakeDocuments);
        Assert.Empty(context.AuditEvents);
    }

    [Fact]
    public async Task CreateFromSampleEndpoint_ReturnsNotFoundForUnknownSample()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await IntakeDocumentEndpoints.CreateFromSampleAsync(
            new CreateIntakeDocumentFromSampleRequest("unknown-sample"),
            service,
            CancellationToken.None);

        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }

    private static IntakeDocumentService CreateService(WorkbenchDbContext context)
    {
        return new IntakeDocumentService(context, new InMemorySampleDocumentCatalog());
    }

    private static WorkbenchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WorkbenchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WorkbenchDbContext(options);
    }
}
