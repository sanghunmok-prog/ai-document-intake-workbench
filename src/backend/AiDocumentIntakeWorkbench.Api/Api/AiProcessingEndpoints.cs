using AiDocumentIntakeWorkbench.Api.AiProcessing;

namespace AiDocumentIntakeWorkbench.Api.Api;

public static class AiProcessingEndpoints
{
    public static IEndpointRouteBuilder MapAiProcessingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/intake-documents/{id:guid}/process-ai", ProcessAsync);

        return app;
    }

    public static async Task<IResult> ProcessAsync(
        Guid id,
        AiProcessingService aiProcessingService,
        CancellationToken cancellationToken)
    {
        var result = await aiProcessingService.ProcessAsync(id, cancellationToken);

        if (result.Succeeded && result.Summary is not null)
        {
            return TypedResults.Ok(result.Summary);
        }

        return result.Error switch
        {
            ProcessIntakeDocumentError.DocumentNotFound => TypedResults.NotFound(
                new ErrorResponse(result.ErrorMessage ?? "Intake document was not found.")),
            ProcessIntakeDocumentError.AlreadyProcessed => TypedResults.Conflict(
                new ErrorResponse(result.ErrorMessage ?? "Intake document has already been processed.")),
            ProcessIntakeDocumentError.InvalidWorkflowStatus => TypedResults.Conflict(
                new ErrorResponse(result.ErrorMessage ?? "Intake document is not in a processable status.")),
            ProcessIntakeDocumentError.AiProcessingFailed => TypedResults.BadRequest(
                new ErrorResponse(result.ErrorMessage ?? "AI processing failed.")),
            _ => TypedResults.BadRequest(new ErrorResponse("AI processing could not be completed."))
        };
    }
}
