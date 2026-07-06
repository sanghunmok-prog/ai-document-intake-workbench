using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AiDocumentIntakeWorkbench.Api.Ai;

public sealed class OpenAiDocumentAiProcessor(
    IOptions<OpenAiDocumentProcessorOptions> options,
    IOpenAiStructuredOutputClient structuredOutputClient) : IDocumentAiProcessor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly OpenAiDocumentProcessorOptions options = options.Value;

    public async Task<DocumentAiProcessingResult> ProcessAsync(
        DocumentAiInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.SampleDocumentId))
        {
            return DocumentAiProcessingResult.Failure(DocumentAiProcessingError.MissingSampleDocumentId);
        }

        var configurationError = ValidateConfiguration();

        if (configurationError is not null)
        {
            return configurationError;
        }

        try
        {
            var response = await structuredOutputClient.GetStructuredOutputAsync(
                new OpenAiStructuredOutputRequest(
                    input.SampleDocumentId,
                    input.Scenario,
                    input.Title ?? string.Empty,
                    input.Summary,
                    input.Content),
                options,
                cancellationToken);

            if (!response.Succeeded || string.IsNullOrWhiteSpace(response.JsonOutput))
            {
                return DocumentAiProcessingResult.Failure(DocumentAiProcessingError.ProviderFailure);
            }

            return OpenAiDocumentOutputMapper.Map(response.JsonOutput, input);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return DocumentAiProcessingResult.Failure(DocumentAiProcessingError.ProviderFailure);
        }
    }

    private DocumentAiProcessingResult? ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey)
            || string.IsNullOrWhiteSpace(options.Model)
            || string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return DocumentAiProcessingResult.Failure(DocumentAiProcessingError.ProviderConfiguration);
        }

        return null;
    }

    private static class OpenAiDocumentOutputMapper
    {
        public static DocumentAiProcessingResult Map(string jsonOutput, DocumentAiInput input)
        {
            OpenAiDocumentOutputDto? output;

            try
            {
                output = JsonSerializer.Deserialize<OpenAiDocumentOutputDto>(jsonOutput, SerializerOptions);
            }
            catch (JsonException)
            {
                return DocumentAiProcessingResult.Failure(DocumentAiProcessingError.InvalidStructuredOutput);
            }

            if (output is null
                || string.IsNullOrWhiteSpace(output.DocumentType)
                || output.OverallConfidence is null
                || output.OverallConfidence < 0m
                || output.OverallConfidence > 1m
                || output.ExtractedFields is null
                || string.IsNullOrWhiteSpace(output.SuggestedRouting)
                || string.IsNullOrWhiteSpace(output.Rationale)
                || output.ExtractedFields.Any(field =>
                    string.IsNullOrWhiteSpace(field.Name)
                    || field.Confidence is null
                    || field.Confidence < 0m
                    || field.Confidence > 1m))
            {
                return DocumentAiProcessingResult.Failure(DocumentAiProcessingError.InvalidStructuredOutput);
            }

            var fields = output.ExtractedFields
                .Select(field => new StructuredDocumentField(
                    field.Name!.Trim(),
                    field.Value?.Trim() ?? string.Empty,
                    field.Confidence!.Value))
                .ToArray();

            var sourceSampleDocumentId = string.IsNullOrWhiteSpace(output.SourceSampleDocumentId)
                ? input.SampleDocumentId
                : output.SourceSampleDocumentId.Trim();

            return DocumentAiProcessingResult.Success(new DocumentAiOutput(
                sourceSampleDocumentId!,
                output.DocumentType.Trim(),
                output.OverallConfidence.Value,
                fields,
                NormalizeList(output.MissingFields),
                NormalizeList(output.RiskIndicators),
                output.SuggestedRouting.Trim(),
                output.Rationale.Trim()));
        }

        private static string[] NormalizeList(IReadOnlyList<string>? values)
        {
            return values?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToArray() ?? [];
        }
    }
}

public sealed record OpenAiDocumentOutputDto(
    string? SourceSampleDocumentId,
    string? DocumentType,
    decimal? OverallConfidence,
    IReadOnlyList<OpenAiDocumentFieldDto>? ExtractedFields,
    IReadOnlyList<string>? MissingFields,
    IReadOnlyList<string>? RiskIndicators,
    string? SuggestedRouting,
    string? Rationale);

public sealed record OpenAiDocumentFieldDto(
    string? Name,
    string? Value,
    decimal? Confidence);
