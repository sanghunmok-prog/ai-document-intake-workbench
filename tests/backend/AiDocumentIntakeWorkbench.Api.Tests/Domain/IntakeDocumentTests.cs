using AiDocumentIntakeWorkbench.Api.Domain;

namespace AiDocumentIntakeWorkbench.Api.Tests.Domain;

public sealed class IntakeDocumentTests
{
    [Fact]
    public void Constructor_SetsReceivedDefaults()
    {
        var beforeCreate = DateTime.UtcNow;

        var document = new IntakeDocument("  sample-document.pdf  ");

        Assert.NotEqual(Guid.Empty, document.Id);
        Assert.Equal("sample-document.pdf", document.DisplayName);
        Assert.Equal(WorkflowStatus.Received, document.Status);
        Assert.True(document.CreatedUtc >= beforeCreate);
        Assert.Equal(document.CreatedUtc, document.UpdatedUtc);
        Assert.Empty(document.AuditEvents);
    }

    [Fact]
    public void Constructor_RejectsMissingDisplayName()
    {
        Assert.Throws<ArgumentException>(() => new IntakeDocument(" "));
    }
}
