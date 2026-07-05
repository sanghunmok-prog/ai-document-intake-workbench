namespace AiDocumentIntakeWorkbench.Api.Domain;

public enum ReviewerDecision
{
    Approved = 0,
    Rejected = 1,
    NeedsCorrection = 2
}

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

    public ReviewerDecision? Decision { get; private set; }

    public string? DecidedBy { get; private set; }

    public DateTime? DecidedUtc { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DateTime UpdatedUtc { get; private set; }

    public IntakeDocument? IntakeDocument { get; private set; }

    public void RecordDecision(ReviewerDecision decision, string decidedBy)
    {
        if (string.IsNullOrWhiteSpace(decidedBy))
        {
            throw new ArgumentException("Reviewer is required.", nameof(decidedBy));
        }

        Decision = decision;
        DecidedBy = decidedBy.Trim();
        DecidedUtc = DateTime.UtcNow;
        RequiresHumanReview = false;
        UpdatedUtc = DecidedUtc.Value;
    }
}
