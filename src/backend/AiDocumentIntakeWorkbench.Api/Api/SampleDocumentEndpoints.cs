using AiDocumentIntakeWorkbench.Api.SampleDocuments;

namespace AiDocumentIntakeWorkbench.Api.Api;

public static class SampleDocumentEndpoints
{
    public static IEndpointRouteBuilder MapSampleDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sample-documents", (ISampleDocumentCatalog catalog) =>
        {
            var response = catalog.GetAll()
                .Select(SampleDocumentResponse.From)
                .ToArray();

            return Results.Ok(response);
        });

        return app;
    }
}

public sealed record SampleDocumentResponse(
    string Id,
    string Title,
    string Scenario,
    string Summary,
    string ContentPreview)
{
    public static SampleDocumentResponse From(SampleDocument sampleDocument)
    {
        return new SampleDocumentResponse(
            sampleDocument.Id,
            sampleDocument.Title,
            sampleDocument.Scenario,
            sampleDocument.Summary,
            CreatePreview(sampleDocument.Content));
    }

    private static string CreatePreview(string content)
    {
        const int maxLength = 220;
        var normalized = string.Join(" ", content.Split(
            ['\r', '\n', '\t'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized[..maxLength]}...";
    }
}
