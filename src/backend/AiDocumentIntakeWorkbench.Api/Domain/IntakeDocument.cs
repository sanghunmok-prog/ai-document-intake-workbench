namespace AiDocumentIntakeWorkbench.Api.Domain;

public sealed class IntakeDocument
{
    private readonly List<AuditEvent> _auditEvents = [];

    private IntakeDocument()
    {
    }

    public IntakeDocument(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Document display name is required.", nameof(displayName));
        }

        Id = Guid.NewGuid();
        DisplayName = displayName.Trim();
        Status = WorkflowStatus.Received;
        CreatedUtc = DateTime.UtcNow;
        UpdatedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public WorkflowStatus Status { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DateTime UpdatedUtc { get; private set; }

    public ReviewState? ReviewState { get; private set; }

    public IReadOnlyCollection<AuditEvent> AuditEvents => _auditEvents;
}
