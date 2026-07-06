using AiDocumentIntakeWorkbench.Api.Data;
using AiDocumentIntakeWorkbench.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiDocumentIntakeWorkbench.Api.Review;

public sealed class ReviewWorkflowService(WorkbenchDbContext dbContext)
{
    private const string ReviewerActor = "Reviewer";

    public async Task<ReviewWorkflowResult> UpdateFieldsAsync(
        Guid intakeDocumentId,
        IReadOnlyList<ReviewerFieldUpdate> fieldUpdates,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateFieldUpdates(fieldUpdates);

        if (validationError is not null)
        {
            return validationError;
        }

        var document = await dbContext.IntakeDocuments
            .FirstOrDefaultAsync(item => item.Id == intakeDocumentId, cancellationToken);

        if (document is null)
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.DocumentNotFound,
                "Intake document was not found.");
        }

        if (IsFinalStatus(document.Status))
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.AlreadyFinalized,
                "Review has already been finalized.");
        }

        if (document.Status != WorkflowStatus.AwaitingReview)
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.NotReviewable,
                "Intake document is not ready for reviewer updates.");
        }

        var processingResult = await dbContext.DocumentProcessingResults
            .OrderByDescending(result => result.CreatedUtc)
            .FirstOrDefaultAsync(result => result.IntakeDocumentId == intakeDocumentId, cancellationToken);

        if (processingResult is null)
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.NotReviewable,
                "Intake document does not have AI processing output to review.");
        }

        var fields = await dbContext.ExtractedDocumentFields
            .Where(field => field.DocumentProcessingResultId == processingResult.Id)
            .ToArrayAsync(cancellationToken);

        var fieldLookup = fields.ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase);
        var normalizedUpdates = fieldUpdates
            .Select(update => new ReviewerFieldUpdate(
                update.FieldName.Trim(),
                update.ReviewedValue.Trim()))
            .ToArray();

        var unknownField = normalizedUpdates
            .FirstOrDefault(update => !fieldLookup.ContainsKey(update.FieldName));

        if (unknownField is not null)
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.UnknownField,
                $"Field '{unknownField.FieldName}' was not found for this review item.");
        }

        if (dbContext.Database.IsRelational())
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var transactionResult = await PersistFieldUpdatesAsync(
                document,
                fieldLookup,
                normalizedUpdates,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return transactionResult;
        }

        return await PersistFieldUpdatesAsync(
            document,
            fieldLookup,
            normalizedUpdates,
            cancellationToken);
    }

    public async Task<ReviewWorkflowResult> RecordDecisionAsync(
        Guid intakeDocumentId,
        string? decision,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseReviewerDecision(decision, out var parsedDecision))
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.InvalidDecision,
                "Reviewer decision must be Approved, Rejected, or NeedsCorrection.");
        }

        var document = await dbContext.IntakeDocuments
            .FirstOrDefaultAsync(item => item.Id == intakeDocumentId, cancellationToken);

        if (document is null)
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.DocumentNotFound,
                "Intake document was not found.");
        }

        if (IsFinalStatus(document.Status))
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.AlreadyFinalized,
                "Review has already been finalized.");
        }

        if (document.Status != WorkflowStatus.AwaitingReview)
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.NotReviewable,
                "Intake document is not ready for reviewer decision.");
        }

        var reviewState = await dbContext.ReviewStates
            .FirstOrDefaultAsync(state => state.IntakeDocumentId == intakeDocumentId, cancellationToken);

        if (reviewState is null)
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.NotReviewable,
                "Intake document does not have review state.");
        }

        if (dbContext.Database.IsRelational())
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var transactionResult = await PersistDecisionAsync(
                document,
                reviewState,
                parsedDecision,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return transactionResult;
        }

        return await PersistDecisionAsync(
            document,
            reviewState,
            parsedDecision,
            cancellationToken);
    }

    private async Task<ReviewWorkflowResult> PersistFieldUpdatesAsync(
        IntakeDocument document,
        IReadOnlyDictionary<string, ExtractedDocumentField> fieldLookup,
        IReadOnlyList<ReviewerFieldUpdate> fieldUpdates,
        CancellationToken cancellationToken)
    {
        foreach (var update in fieldUpdates)
        {
            var field = fieldLookup[update.FieldName];
            var previousValue = field.ReviewedValue ?? field.Value;
            field.ApplyReviewedValue(update.ReviewedValue, ReviewerActor);
            dbContext.AuditEvents.Add(new AuditEvent(
                document.Id,
                "ReviewerFieldEdited",
                $"Reviewer updated field '{field.Name}' from '{previousValue}' to '{field.ReviewedValue}'."));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ReviewWorkflowResult.Success(new ReviewWorkflowResponse(
            document.Id,
            document.Status.ToString(),
            Decision: null,
            EditedFieldCount: fieldUpdates.Count));
    }

    private async Task<ReviewWorkflowResult> PersistDecisionAsync(
        IntakeDocument document,
        ReviewState reviewState,
        ReviewerDecision decision,
        CancellationToken cancellationToken)
    {
        var originalStatus = document.Status;
        var nextStatus = decision switch
        {
            ReviewerDecision.Approved => WorkflowStatus.Approved,
            ReviewerDecision.Rejected => WorkflowStatus.Rejected,
            ReviewerDecision.NeedsCorrection => WorkflowStatus.NeedsCorrection,
            _ => throw new InvalidOperationException("Unsupported reviewer decision.")
        };

        reviewState.RecordDecision(decision, ReviewerActor);
        dbContext.AuditEvents.Add(new AuditEvent(
            document.Id,
            "ReviewerDecisionRecorded",
            $"Reviewer recorded decision '{decision}'."));

        if (document.ChangeStatus(nextStatus))
        {
            dbContext.AuditEvents.Add(new AuditEvent(
                document.Id,
                "WorkflowStatusChanged",
                $"Workflow status changed from '{originalStatus}' to '{document.Status}'."));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ReviewWorkflowResult.Success(new ReviewWorkflowResponse(
            document.Id,
            document.Status.ToString(),
            decision.ToString(),
            EditedFieldCount: 0));
    }

    private static ReviewWorkflowResult? ValidateFieldUpdates(IReadOnlyList<ReviewerFieldUpdate> fieldUpdates)
    {
        if (fieldUpdates.Count == 0)
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.InvalidFieldUpdate,
                "At least one field update is required.");
        }

        if (fieldUpdates.Any(update => string.IsNullOrWhiteSpace(update.FieldName)))
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.InvalidFieldUpdate,
                "Field name is required for every update.");
        }

        if (fieldUpdates.Any(update => string.IsNullOrWhiteSpace(update.ReviewedValue)))
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.InvalidFieldUpdate,
                "Reviewed value is required for every update.");
        }

        var duplicateFieldName = fieldUpdates
            .GroupBy(update => update.FieldName.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateFieldName is not null)
        {
            return ReviewWorkflowResult.Failure(
                ReviewWorkflowError.InvalidFieldUpdate,
                $"Field '{duplicateFieldName}' was included more than once.");
        }

        return null;
    }

    private static bool IsFinalStatus(WorkflowStatus status)
    {
        return status is WorkflowStatus.Approved
            or WorkflowStatus.Rejected
            or WorkflowStatus.NeedsCorrection
            or WorkflowStatus.Closed;
    }

    private static bool TryParseReviewerDecision(string? decision, out ReviewerDecision parsedDecision)
    {
        parsedDecision = default;

        if (string.IsNullOrWhiteSpace(decision))
        {
            return false;
        }

        var normalizedDecision = decision.Trim();
        var isNamedDecision = Enum.GetNames<ReviewerDecision>()
            .Any(name => string.Equals(name, normalizedDecision, StringComparison.OrdinalIgnoreCase));

        return isNamedDecision
            && Enum.TryParse(normalizedDecision, ignoreCase: true, out parsedDecision);
    }
}

public sealed record ReviewerFieldUpdate(
    string FieldName,
    string ReviewedValue);

public sealed record ReviewWorkflowResponse(
    Guid IntakeDocumentId,
    string WorkflowStatus,
    string? Decision,
    int EditedFieldCount);

public sealed class ReviewWorkflowResult
{
    private ReviewWorkflowResult(
        bool succeeded,
        ReviewWorkflowResponse? response,
        ReviewWorkflowError? error,
        string? errorMessage)
    {
        Succeeded = succeeded;
        Response = response;
        Error = error;
        ErrorMessage = errorMessage;
    }

    public bool Succeeded { get; }

    public ReviewWorkflowResponse? Response { get; }

    public ReviewWorkflowError? Error { get; }

    public string? ErrorMessage { get; }

    public static ReviewWorkflowResult Success(ReviewWorkflowResponse response)
    {
        return new ReviewWorkflowResult(true, response, null, null);
    }

    public static ReviewWorkflowResult Failure(ReviewWorkflowError error, string errorMessage)
    {
        return new ReviewWorkflowResult(false, null, error, errorMessage);
    }
}

public enum ReviewWorkflowError
{
    DocumentNotFound,
    InvalidFieldUpdate,
    UnknownField,
    InvalidDecision,
    NotReviewable,
    AlreadyFinalized
}
