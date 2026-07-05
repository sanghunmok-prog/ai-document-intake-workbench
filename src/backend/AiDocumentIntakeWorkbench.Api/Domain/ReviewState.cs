namespace AiDocumentIntakeWorkbench.Api.Domain;

public sealed class ReviewState
{
    private ReviewState()
    {
    }

    public ReviewState(Guid intakeDocumentId)
    {
        if (intakeDocumentId == Guid.Empty)
        {
            throw new ArgumentException("Document identifier is required.", nameof(intakeDocumentId));
        }

        Id = Guid.NewGuid();
        IntakeDocumentId = intakeDocumentId;
        RequiresHumanReview = true;
        CreatedUtc = DateTime.UtcNow;
        UpdatedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }

    public Guid IntakeDocumentId { get; private set; }

    public bool RequiresHumanReview { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DateTime UpdatedUtc { get; private set; }

    public IntakeDocument? IntakeDocument { get; private set; }
}
