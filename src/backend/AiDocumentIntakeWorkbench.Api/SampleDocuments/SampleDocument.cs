namespace AiDocumentIntakeWorkbench.Api.SampleDocuments;

public sealed record SampleDocument(
    string Id,
    string Title,
    string Scenario,
    string Summary,
    string Content);
