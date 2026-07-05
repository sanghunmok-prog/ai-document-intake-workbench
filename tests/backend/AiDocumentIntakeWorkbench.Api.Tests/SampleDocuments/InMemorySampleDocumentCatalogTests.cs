using AiDocumentIntakeWorkbench.Api.SampleDocuments;

namespace AiDocumentIntakeWorkbench.Api.Tests.SampleDocuments;

public sealed class InMemorySampleDocumentCatalogTests
{
    [Fact]
    public void GetAll_ReturnsExpectedPr03SampleIds()
    {
        var catalog = new InMemorySampleDocumentCatalog();

        var sampleIds = catalog.GetAll()
            .Select(sample => sample.Id)
            .ToArray();

        Assert.Equal(
            [
                "clean-high-confidence",
                "missing-low-confidence",
                "conflicting-inconsistent"
            ],
            sampleIds);
    }

    [Fact]
    public void FindById_MatchesStableIdIgnoringCase()
    {
        var catalog = new InMemorySampleDocumentCatalog();

        var sample = catalog.FindById("CLEAN-HIGH-CONFIDENCE");

        Assert.NotNull(sample);
        Assert.Equal("clean-high-confidence", sample.Id);
    }
}
