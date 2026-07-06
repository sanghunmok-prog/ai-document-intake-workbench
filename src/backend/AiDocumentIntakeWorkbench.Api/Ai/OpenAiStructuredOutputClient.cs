using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AiDocumentIntakeWorkbench.Api.Ai;

public interface IOpenAiStructuredOutputClient
{
    Task<OpenAiStructuredOutputResponse> GetStructuredOutputAsync(
        OpenAiStructuredOutputRequest request,
        OpenAiDocumentProcessorOptions options,
        CancellationToken cancellationToken = default);
}

public sealed class HttpOpenAiStructuredOutputClient(HttpClient httpClient) : IOpenAiStructuredOutputClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<OpenAiStructuredOutputResponse> GetStructuredOutputAsync(
        OpenAiStructuredOutputRequest request,
        OpenAiDocumentProcessorOptions options,
        CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        httpRequest.Content = JsonContent.Create(
            CreatePayload(request, options.Model!),
            options: SerializerOptions);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return OpenAiStructuredOutputResponse.Failure();
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var responseText = TryExtractResponseText(document.RootElement);

        return string.IsNullOrWhiteSpace(responseText)
            ? OpenAiStructuredOutputResponse.Failure()
            : OpenAiStructuredOutputResponse.Success(responseText);
    }

    private static object CreatePayload(OpenAiStructuredOutputRequest request, string model)
    {
        return new
        {
            model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = OpenAiDocumentPrompt.SystemInstructions
                        }
                    }
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_text",
                            text = OpenAiDocumentPrompt.CreateUserInput(request)
                        }
                    }
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "document_intake_workbench_ai_result",
                    schema = OpenAiDocumentPrompt.OutputSchema,
                    strict = true
                }
            }
        };
    }

    private static string? TryExtractResponseText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText)
            && outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString();
        }

        if (!root.TryGetProperty("output", out var output)
            || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text)
                    && text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString();
                }
            }
        }

        return null;
    }
}

public sealed record OpenAiStructuredOutputRequest(
    string SampleDocumentId,
    string? Scenario,
    string DisplayName,
    string? Summary,
    string? Content);

public sealed record OpenAiStructuredOutputResponse(
    bool Succeeded,
    string? JsonOutput)
{
    public static OpenAiStructuredOutputResponse Success(string jsonOutput)
    {
        return new OpenAiStructuredOutputResponse(true, jsonOutput);
    }

    public static OpenAiStructuredOutputResponse Failure()
    {
        return new OpenAiStructuredOutputResponse(false, null);
    }
}
