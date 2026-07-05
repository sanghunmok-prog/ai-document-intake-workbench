using AiDocumentIntakeWorkbench.Api.Domain;

namespace AiDocumentIntakeWorkbench.Api.Tests.Domain;

public sealed class AuditEventTests
{
    [Fact]
    public void Constructor_SetsDocumentRelationshipAndText()
    {
        var documentId = Guid.NewGuid();

        var auditEvent = new AuditEvent(documentId, "  DocumentReceived  ", "  Document shell record created.  ");

        Assert.NotEqual(Guid.Empty, auditEvent.Id);
        Assert.Equal(documentId, auditEvent.IntakeDocumentId);
        Assert.Equal("DocumentReceived", auditEvent.EventType);
        Assert.Equal("Document shell record created.", auditEvent.Message);
        Assert.True(auditEvent.CreatedUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_RejectsMissingRequiredValues()
    {
        Assert.Throws<ArgumentException>(() => new AuditEvent(Guid.Empty, "DocumentReceived", "Created."));
        Assert.Throws<ArgumentException>(() => new AuditEvent(Guid.NewGuid(), " ", "Created."));
        Assert.Throws<ArgumentException>(() => new AuditEvent(Guid.NewGuid(), "DocumentReceived", " "));
    }
}
