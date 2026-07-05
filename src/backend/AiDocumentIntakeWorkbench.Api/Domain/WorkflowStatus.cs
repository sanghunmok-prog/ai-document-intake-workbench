namespace AiDocumentIntakeWorkbench.Api.Domain;

public enum WorkflowStatus
{
    Received = 0,
    Processing = 1,
    AwaitingReview = 2,
    Closed = 3,
    Approved = 4,
    Rejected = 5,
    NeedsCorrection = 6
}
