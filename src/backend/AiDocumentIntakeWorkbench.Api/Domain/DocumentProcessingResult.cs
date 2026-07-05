namespace AiDocumentIntakeWorkbench.Api.Domain;

public sealed class DocumentProcessingResult
{
    private DocumentProcessingResult()
    {
    }

    public DocumentProcessingResult(
        Guid intakeDocumentId,
        string sourceSampleDocumentId,
        string documentType,
        decimal overallConfidence,
        string suggestedRouting,
        string rationale)
    {
        if (intakeDocumentId == Guid.Empty)
        {
            throw new ArgumentException("Document identifier is required.", nameof(intakeDocumentId));
        }

        if (string.IsNullOrWhiteSpace(sourceSampleDocumentId))
        {
            throw new ArgumentException("Source sample document identifier is required.", nameof(sourceSampleDocumentId));
        }

        if (string.IsNullOrWhiteSpace(documentType))
        {
            throw new ArgumentException("Document type is required.", nameof(documentType));
        }

        Id = Guid.NewGuid();
        IntakeDocumentId = intakeDocumentId;
        SourceSampleDocumentId = sourceSampleDocumentId.Trim();
        DocumentType = documentType.Trim();
        OverallConfidence = overallConfidence;
        SuggestedRouting = NormalizeRequired(suggestedRouting, nameof(suggestedRouting));
        Rationale = NormalizeRequired(rationale, nameof(rationale));
        CreatedUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid IntakeDocumentId { get; private set; }

    public string SourceSampleDocumentId { get; private set; } = string.Empty;

    public string DocumentType { get; private set; } = string.Empty;

    public decimal OverallConfidence { get; private set; }

    public string SuggestedRouting { get; private set; } = string.Empty;

    public string Rationale { get; private set; } = string.Empty;

    public DateTime CreatedUtc { get; private set; }

    public IntakeDocument? IntakeDocument { get; private set; }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
