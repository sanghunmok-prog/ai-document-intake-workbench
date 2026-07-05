namespace AiDocumentIntakeWorkbench.Api.Domain;

public sealed class AuditEvent
{
    private AuditEvent()
    {
    }

    public AuditEvent(Guid intakeDocumentId, string eventType, string message)
    {
        if (intakeDocumentId == Guid.Empty)
        {
            throw new ArgumentException("Document identifier is required.", nameof(intakeDocumentId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Audit event type is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Audit event message is required.", nameof(message));
        }

        Id = Guid.NewGuid();
        IntakeDocumentId = intakeDocumentId;
        EventType = eventType.Trim();
        Message = message.Trim();
        CreatedUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid IntakeDocumentId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public DateTime CreatedUtc { get; private set; }

    public IntakeDocument? IntakeDocument { get; private set; }
}
