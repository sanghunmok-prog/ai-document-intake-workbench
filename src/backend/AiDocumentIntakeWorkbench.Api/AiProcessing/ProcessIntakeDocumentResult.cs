namespace AiDocumentIntakeWorkbench.Api.AiProcessing;

public enum ProcessIntakeDocumentError
{
    DocumentNotFound,
    AlreadyProcessed,
    InvalidWorkflowStatus,
    AiProcessingFailed
}

public sealed record ProcessIntakeDocumentResult(
    ProcessedIntakeDocumentSummary? Summary,
    ProcessIntakeDocumentError? Error,
    string? ErrorMessage)
{
    public bool Succeeded => Summary is not null && Error is null;

    public static ProcessIntakeDocumentResult Success(ProcessedIntakeDocumentSummary summary)
    {
        return new ProcessIntakeDocumentResult(summary, null, null);
    }

    public static ProcessIntakeDocumentResult Failure(
        ProcessIntakeDocumentError error,
        string errorMessage)
    {
        return new ProcessIntakeDocumentResult(null, error, errorMessage);
    }
}

public sealed record ProcessedIntakeDocumentSummary(
    Guid IntakeDocumentId,
    string WorkflowStatus,
    string DocumentType,
    decimal OverallConfidence,
    IReadOnlyList<ProcessedFieldSummary> ExtractedFields,
    IReadOnlyList<ValidationFlagSummary> ValidationFlags,
    string SuggestedRouting,
    string Rationale);

public sealed record ProcessedFieldSummary(
    string Name,
    string Value,
    decimal Confidence);

public sealed record ValidationFlagSummary(
    string FlagType,
    string Severity,
    string? FieldName,
    string Message);
