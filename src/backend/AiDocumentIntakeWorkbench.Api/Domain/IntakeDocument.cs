namespace AiDocumentIntakeWorkbench.Api.Domain;

public sealed class IntakeDocument
{
    private readonly List<AuditEvent> _auditEvents = [];

    private IntakeDocument()
    {
    }

    public IntakeDocument(
        string displayName,
        string? sampleDocumentId = null,
        string? scenario = null,
        string? summary = null,
        string? documentText = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Document display name is required.", nameof(displayName));
        }

        Id = Guid.NewGuid();
        DisplayName = displayName.Trim();
        SampleDocumentId = NormalizeOptional(sampleDocumentId);
        Scenario = NormalizeOptional(scenario);
        Summary = NormalizeOptional(summary);
        DocumentText = NormalizeOptional(documentText);
        Status = WorkflowStatus.Received;
        CreatedUtc = DateTime.UtcNow;
        UpdatedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string? SampleDocumentId { get; private set; }

    public string? Scenario { get; private set; }

    public string? Summary { get; private set; }

    public string? DocumentText { get; private set; }

    public WorkflowStatus Status { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DateTime UpdatedUtc { get; private set; }

    public ReviewState? ReviewState { get; private set; }

    public IReadOnlyCollection<AuditEvent> AuditEvents => _auditEvents;

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
