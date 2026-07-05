namespace AiDocumentIntakeWorkbench.Api.Ai;

public sealed record DocumentAiOutput(
    string SourceSampleDocumentId,
    string DocumentType,
    decimal OverallConfidence,
    IReadOnlyList<StructuredDocumentField> Fields,
    IReadOnlyList<string> MissingFields,
    IReadOnlyList<string> RiskIndicators,
    string SuggestedRouting,
    string Rationale);

public sealed record StructuredDocumentField(
    string Name,
    string Value,
    decimal Confidence);
