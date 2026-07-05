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
    public void Constructor_TrimsSampleMetadata()
    {
        var document = new IntakeDocument(
            "Sample",
            " clean-high-confidence ",
            " Complete invoice scenario ",
            " Complete summary. ",
            " Document text. ");

        Assert.Equal("clean-high-confidence", document.SampleDocumentId);
        Assert.Equal("Complete invoice scenario", document.Scenario);
        Assert.Equal("Complete summary.", document.Summary);
        Assert.Equal("Document text.", document.DocumentText);
    }

    [Fact]
    public void ChangeStatus_UpdatesStatusAndTimestamp()
    {
        var document = new IntakeDocument("Sample");
        var originalUpdatedUtc = document.UpdatedUtc;

        var changed = document.ChangeStatus(WorkflowStatus.AwaitingReview);

        Assert.True(changed);
        Assert.Equal(WorkflowStatus.AwaitingReview, document.Status);
        Assert.True(document.UpdatedUtc >= originalUpdatedUtc);
    }

    [Fact]
    public void ChangeStatus_ReturnsFalseWhenStatusIsUnchanged()
    {
        var document = new IntakeDocument("Sample");
        var originalUpdatedUtc = document.UpdatedUtc;

        var changed = document.ChangeStatus(WorkflowStatus.Received);

        Assert.False(changed);
        Assert.Equal(originalUpdatedUtc, document.UpdatedUtc);
    }

    [Fact]
    public void Constructor_RejectsMissingDisplayName()
    {
        Assert.Throws<ArgumentException>(() => new IntakeDocument(" "));
    }
}
