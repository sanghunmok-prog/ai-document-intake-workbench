namespace AiDocumentIntakeWorkbench.Api.Ai;

public enum DocumentAiProcessingError
{
    MissingSampleDocumentId,
    UnsupportedSampleDocument,
    ProviderConfiguration,
    ProviderFailure,
    InvalidStructuredOutput
}

public sealed record DocumentAiProcessingResult(
    DocumentAiOutput? Output,
    DocumentAiProcessingError? Error)
{
    public bool Succeeded => Output is not null && Error is null;

    public static DocumentAiProcessingResult Success(DocumentAiOutput output)
    {
        return new DocumentAiProcessingResult(output, null);
    }

    public static DocumentAiProcessingResult Failure(DocumentAiProcessingError error)
    {
        return new DocumentAiProcessingResult(null, error);
    }
}
