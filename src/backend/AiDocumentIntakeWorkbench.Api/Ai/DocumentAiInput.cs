using AiDocumentIntakeWorkbench.Api.SampleDocuments;

namespace AiDocumentIntakeWorkbench.Api.Ai;

public sealed record DocumentAiInput(
    string? SampleDocumentId,
    string? Scenario,
    string? Title,
    string? Summary,
    string? Content)
{
    public static DocumentAiInput FromSample(SampleDocument sampleDocument)
    {
        return new DocumentAiInput(
            sampleDocument.Id,
            sampleDocument.Scenario,
            sampleDocument.Title,
            sampleDocument.Summary,
            sampleDocument.Content);
    }
}
