using AiDocumentIntakeWorkbench.Api.Ai;
using AiDocumentIntakeWorkbench.Api.AiProcessing;
using AiDocumentIntakeWorkbench.Api.Data;
using AiDocumentIntakeWorkbench.Api.Domain;
using AiDocumentIntakeWorkbench.Api.Intake;
using AiDocumentIntakeWorkbench.Api.SampleDocuments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AiDocumentIntakeWorkbench.Api.Tests.Ai;

public sealed class OpenAiDocumentAiProcessorTests
{
    [Fact]
    public void AddDocumentAiProcessor_DefaultMode_ResolvesMockProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddDocumentAiProcessor(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var processor = serviceProvider.GetRequiredService<IDocumentAiProcessor>();

        Assert.IsType<DeterministicMockDocumentAiProcessor>(processor);
    }

    [Fact]
    public void AddDocumentAiProcessor_OpenAiMode_ResolvesOpenAiProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiProvider:Mode"] = AiProviderModes.OpenAI,
                ["OpenAI:ApiKey"] = "unit-test-key",
                ["OpenAI:Model"] = "unit-test-model"
            })
            .Build();

        services.AddDocumentAiProcessor(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var processor = serviceProvider.GetRequiredService<IDocumentAiProcessor>();

        Assert.IsType<OpenAiDocumentAiProcessor>(processor);
    }

    [Fact]
    public async Task ProcessAsync_OpenAiModeMissingApiKey_ReturnsConfigurationErrorWithoutCallingProvider()
    {
        var fakeClient = new FakeOpenAiStructuredOutputClient(OpenAiStructuredOutputResponse.Success(CreateValidJson()));
        var processor = CreateProcessor(fakeClient, apiKey: null, model: "unit-test-model");

        var result = await processor.ProcessAsync(CreateInput(SampleDocumentIds.CleanHighConfidence));

        Assert.False(result.Succeeded);
        Assert.Equal(DocumentAiProcessingError.ProviderConfiguration, result.Error);
        Assert.Equal(0, fakeClient.Calls);
    }

    [Fact]
    public async Task ProcessAsync_OpenAiModeMissingModel_ReturnsConfigurationErrorWithoutCallingProvider()
    {
        var fakeClient = new FakeOpenAiStructuredOutputClient(OpenAiStructuredOutputResponse.Success(CreateValidJson()));
        var processor = CreateProcessor(fakeClient, apiKey: "unit-test-key", model: null);

        var result = await processor.ProcessAsync(CreateInput(SampleDocumentIds.CleanHighConfidence));

        Assert.False(result.Succeeded);
        Assert.Equal(DocumentAiProcessingError.ProviderConfiguration, result.Error);
        Assert.Equal(0, fakeClient.Calls);
    }

    [Fact]
    public async Task ProcessAsync_ValidStructuredOutput_MapsToProviderNeutralAiOutput()
    {
        var fakeClient = new FakeOpenAiStructuredOutputClient(OpenAiStructuredOutputResponse.Success(CreateValidJson()));
        var processor = CreateProcessor(fakeClient);

        var result = await processor.ProcessAsync(CreateInput(SampleDocumentIds.CleanHighConfidence));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Output);
        Assert.Equal(SampleDocumentIds.CleanHighConfidence, result.Output.SourceSampleDocumentId);
        Assert.Equal("VendorInvoice", result.Output.DocumentType);
        Assert.Equal(0.93m, result.Output.OverallConfidence);
        Assert.Contains(result.Output.Fields, field => field.Name == "InvoiceNumber" && field.Value == "INV-10482");
        Assert.Empty(result.Output.MissingFields);
        Assert.Contains("Ready for human review", result.Output.SuggestedRouting);
        Assert.Equal(1, fakeClient.Calls);
    }

    [Fact]
    public async Task ProcessAsync_InvalidStructuredOutput_ReturnsControlledError()
    {
        var fakeClient = new FakeOpenAiStructuredOutputClient(OpenAiStructuredOutputResponse.Success("""
            {
              "sourceSampleDocumentId": "clean-high-confidence",
              "documentType": "",
              "overallConfidence": 1.3,
              "extractedFields": [],
              "missingFields": [],
              "riskIndicators": [],
              "suggestedRouting": "HumanReviewRequired",
              "rationale": "Invalid confidence and missing document type."
            }
            """));
        var processor = CreateProcessor(fakeClient);

        var result = await processor.ProcessAsync(CreateInput(SampleDocumentIds.CleanHighConfidence));

        Assert.False(result.Succeeded);
        Assert.Equal(DocumentAiProcessingError.InvalidStructuredOutput, result.Error);
    }

    [Fact]
    public async Task ProcessAsync_ProviderFailure_ReturnsControlledError()
    {
        var fakeClient = new FakeOpenAiStructuredOutputClient(OpenAiStructuredOutputResponse.Failure());
        var processor = CreateProcessor(fakeClient);

        var result = await processor.ProcessAsync(CreateInput(SampleDocumentIds.CleanHighConfidence));

        Assert.False(result.Succeeded);
        Assert.Equal(DocumentAiProcessingError.ProviderFailure, result.Error);
    }

    [Fact]
    public async Task AiProcessingService_OpenAiProviderFailure_DoesNotMutateWorkflowState()
    {
        using var context = CreateContext();
        var document = await CreateIntakeDocumentAsync(context, SampleDocumentIds.CleanHighConfidence);
        var fakeClient = new FakeOpenAiStructuredOutputClient(OpenAiStructuredOutputResponse.Failure());
        var service = new AiProcessingService(context, CreateProcessor(fakeClient));

        var result = await service.ProcessAsync(document.Id);

        Assert.False(result.Succeeded);
        Assert.Equal(ProcessIntakeDocumentError.AiProcessingFailed, result.Error);
        Assert.Equal(WorkflowStatus.Received, (await context.IntakeDocuments.SingleAsync()).Status);
        Assert.Empty(context.DocumentProcessingResults);
        Assert.Empty(context.ExtractedDocumentFields);
        Assert.Empty(context.ValidationFlags);
        Assert.Single(context.AuditEvents);
        Assert.Equal("SampleDocumentSelected", context.AuditEvents.Single().EventType);
    }

    private static OpenAiDocumentAiProcessor CreateProcessor(
        IOpenAiStructuredOutputClient client,
        string? apiKey = "unit-test-key",
        string? model = "unit-test-model")
    {
        return new OpenAiDocumentAiProcessor(
            Options.Create(new OpenAiDocumentProcessorOptions
            {
                ApiKey = apiKey,
                Model = model
            }),
            client);
    }

    private static string CreateValidJson()
    {
        return """
            {
              "sourceSampleDocumentId": "clean-high-confidence",
              "documentType": "VendorInvoice",
              "overallConfidence": 0.93,
              "extractedFields": [
                {
                  "name": "InvoiceNumber",
                  "value": "INV-10482",
                  "confidence": 0.96
                },
                {
                  "name": "TotalDue",
                  "value": "$4,218.40",
                  "confidence": 0.92
                }
              ],
              "missingFields": [],
              "riskIndicators": [],
              "suggestedRouting": "Ready for human review",
              "rationale": "The document contains invoice identifiers and payment details."
            }
            """;
    }

    private static DocumentAiInput CreateInput(string sampleDocumentId)
    {
        var sample = new InMemorySampleDocumentCatalog().FindById(sampleDocumentId);
        Assert.NotNull(sample);

        return DocumentAiInput.FromSample(sample);
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

    private sealed class FakeOpenAiStructuredOutputClient(OpenAiStructuredOutputResponse response) : IOpenAiStructuredOutputClient
    {
        public int Calls { get; private set; }

        public Task<OpenAiStructuredOutputResponse> GetStructuredOutputAsync(
            OpenAiStructuredOutputRequest request,
            OpenAiDocumentProcessorOptions options,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(response);
        }
    }
}
