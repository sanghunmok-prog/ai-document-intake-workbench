namespace AiDocumentIntakeWorkbench.Api.Ai;

public interface IDocumentAiProcessor
{
    Task<DocumentAiProcessingResult> ProcessAsync(
        DocumentAiInput input,
        CancellationToken cancellationToken = default);
}
