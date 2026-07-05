namespace AiDocumentIntakeWorkbench.Api.Domain;

public enum ValidationFlagType
{
    MissingRequiredField = 0,
    LowConfidence = 1,
    InconsistentData = 2,
    RiskIndicator = 3,
    IncompleteStructuredOutput = 4
}

public enum ValidationFlagSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public sealed class ValidationFlag
{
    private ValidationFlag()
    {
    }

    public ValidationFlag(
        Guid intakeDocumentId,
        Guid documentProcessingResultId,
        ValidationFlagType flagType,
        ValidationFlagSeverity severity,
        string message,
        string? fieldName = null)
    {
        if (intakeDocumentId == Guid.Empty)
        {
            throw new ArgumentException("Document identifier is required.", nameof(intakeDocumentId));
        }

        if (documentProcessingResultId == Guid.Empty)
        {
            throw new ArgumentException("Processing result identifier is required.", nameof(documentProcessingResultId));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Validation flag message is required.", nameof(message));
        }

        Id = Guid.NewGuid();
        IntakeDocumentId = intakeDocumentId;
        DocumentProcessingResultId = documentProcessingResultId;
        FlagType = flagType;
        Severity = severity;
        Message = message.Trim();
        FieldName = string.IsNullOrWhiteSpace(fieldName) ? null : fieldName.Trim();
        CreatedUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid IntakeDocumentId { get; private set; }

    public Guid DocumentProcessingResultId { get; private set; }

    public ValidationFlagType FlagType { get; private set; }

    public ValidationFlagSeverity Severity { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public string? FieldName { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public IntakeDocument? IntakeDocument { get; private set; }

    public DocumentProcessingResult? DocumentProcessingResult { get; private set; }
}
