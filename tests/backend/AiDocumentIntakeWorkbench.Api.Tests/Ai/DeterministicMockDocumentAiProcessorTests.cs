using System.Text.Json;
using AiDocumentIntakeWorkbench.Api.Ai;
using AiDocumentIntakeWorkbench.Api.SampleDocuments;

namespace AiDocumentIntakeWorkbench.Api.Tests.Ai;

public sealed class DeterministicMockDocumentAiProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ReturnsStructuredHighConfidenceOutputForCleanSample()
    {
        var processor = new DeterministicMockDocumentAiProcessor();

        var result = await processor.ProcessAsync(CreateInput(SampleDocumentIds.CleanHighConfidence));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Output);
        Assert.Equal(SampleDocumentIds.CleanHighConfidence, result.Output.SourceSampleDocumentId);
        Assert.Equal("VendorInvoice", result.Output.DocumentType);
        Assert.True(result.Output.OverallConfidence >= 0.90m);
        Assert.Contains(result.Output.Fields, field => field.Name == "InvoiceNumber" && field.Value == "INV-10482");
        Assert.Contains(result.Output.Fields, field => field.Name == "TotalDue" && field.Value == "$4,218.40");
        Assert.Empty(result.Output.MissingFields);
        Assert.Empty(result.Output.RiskIndicators);
        Assert.Equal("ReadyForStandardHumanReview", result.Output.SuggestedRouting);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsLowerConfidenceOutputWithMissingFieldsForIncompleteSample()
    {
        var processor = new DeterministicMockDocumentAiProcessor();

        var result = await processor.ProcessAsync(CreateInput(SampleDocumentIds.MissingLowConfidence));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Output);
        Assert.Equal("FacilitiesServiceRequest", result.Output.DocumentType);
        Assert.True(result.Output.OverallConfidence < 0.75m);
        Assert.Contains("AccountReference", result.Output.MissingFields);
        Assert.NotEmpty(result.Output.RiskIndicators);
        Assert.Contains(result.Output.RiskIndicators, indicator => indicator.Contains("account reference", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("HumanReviewRequired", result.Output.SuggestedRouting);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsConflictIndicatorsForInconsistentSample()
    {
        var processor = new DeterministicMockDocumentAiProcessor();

        var result = await processor.ProcessAsync(CreateInput(SampleDocumentIds.ConflictingInconsistent));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Output);
        Assert.Equal("PurchaseOrder", result.Output.DocumentType);
        Assert.Contains(result.Output.Fields, field => field.Name == "StatedOrderTotal" && field.Value == "$1,900.00");
        Assert.Contains(result.Output.Fields, field => field.Name == "CalculatedLineTotal" && field.Value == "$1,450.00");
        Assert.Contains(result.Output.RiskIndicators, indicator => indicator.Contains("conflicts", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("HumanReviewRequired", result.Output.SuggestedRouting);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsEquivalentOutputForRepeatedCalls()
    {
        var processor = new DeterministicMockDocumentAiProcessor();
        var input = CreateInput(SampleDocumentIds.CleanHighConfidence);

        var first = await processor.ProcessAsync(input);
        var second = await processor.ProcessAsync(input);

        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
    }

    [Fact]
    public async Task ProcessAsync_CanBeCalledThroughProviderNeutralInterface()
    {
        IDocumentAiProcessor processor = new DeterministicMockDocumentAiProcessor();

        var result = await processor.ProcessAsync(CreateInput(SampleDocumentIds.CleanHighConfidence));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Output);
        Assert.NotEmpty(result.Output.Fields);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsControlledFailureForUnknownSample()
    {
        var processor = new DeterministicMockDocumentAiProcessor();

        var result = await processor.ProcessAsync(new DocumentAiInput(
            "unknown-sample",
            "Unknown scenario",
            "Unknown document",
            "Unknown summary",
            "Unknown content"));

        Assert.False(result.Succeeded);
        Assert.Null(result.Output);
        Assert.Equal(DocumentAiProcessingError.UnsupportedSampleDocument, result.Error);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsControlledFailureForMissingSampleId()
    {
        var processor = new DeterministicMockDocumentAiProcessor();

        var result = await processor.ProcessAsync(new DocumentAiInput(
            " ",
            "Missing identifier scenario",
            "Document",
            "Summary",
            "Content"));

        Assert.False(result.Succeeded);
        Assert.Null(result.Output);
        Assert.Equal(DocumentAiProcessingError.MissingSampleDocumentId, result.Error);
    }

    private static DocumentAiInput CreateInput(string sampleDocumentId)
    {
        var sample = new InMemorySampleDocumentCatalog().FindById(sampleDocumentId);
        Assert.NotNull(sample);

        return DocumentAiInput.FromSample(sample);
    }
}
