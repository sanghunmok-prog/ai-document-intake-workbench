using AiDocumentIntakeWorkbench.Api.Domain;

namespace AiDocumentIntakeWorkbench.Api.Tests.Domain;

public sealed class ReviewStateTests
{
    [Fact]
    public void Constructor_SetsHumanReviewDefaults()
    {
        var documentId = Guid.NewGuid();

        var reviewState = new ReviewState(documentId);

        Assert.NotEqual(Guid.Empty, reviewState.Id);
        Assert.Equal(documentId, reviewState.IntakeDocumentId);
        Assert.True(reviewState.RequiresHumanReview);
        Assert.Equal(reviewState.CreatedUtc, reviewState.UpdatedUtc);
    }

    [Fact]
    public void Constructor_RejectsMissingDocumentId()
    {
        Assert.Throws<ArgumentException>(() => new ReviewState(Guid.Empty));
    }
}
