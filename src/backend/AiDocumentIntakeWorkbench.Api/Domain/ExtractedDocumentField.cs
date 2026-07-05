namespace AiDocumentIntakeWorkbench.Api.Domain;

public sealed class ExtractedDocumentField
{
    private ExtractedDocumentField()
    {
    }

    public ExtractedDocumentField(
        Guid documentProcessingResultId,
        string name,
        string value,
        decimal confidence)
    {
        if (documentProcessingResultId == Guid.Empty)
        {
            throw new ArgumentException("Processing result identifier is required.", nameof(documentProcessingResultId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Field name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        DocumentProcessingResultId = documentProcessingResultId;
        Name = name.Trim();
        Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        Confidence = confidence;
        CreatedUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid DocumentProcessingResultId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public decimal Confidence { get; private set; }

    public string? ReviewedValue { get; private set; }

    public string? ReviewedBy { get; private set; }

    public DateTime? ReviewedUtc { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DocumentProcessingResult? DocumentProcessingResult { get; private set; }

    public void ApplyReviewedValue(string reviewedValue, string reviewedBy)
    {
        if (string.IsNullOrWhiteSpace(reviewedValue))
        {
            throw new ArgumentException("Reviewed value is required.", nameof(reviewedValue));
        }

        if (string.IsNullOrWhiteSpace(reviewedBy))
        {
            throw new ArgumentException("Reviewer is required.", nameof(reviewedBy));
        }

        ReviewedValue = reviewedValue.Trim();
        ReviewedBy = reviewedBy.Trim();
        ReviewedUtc = DateTime.UtcNow;
    }
}
