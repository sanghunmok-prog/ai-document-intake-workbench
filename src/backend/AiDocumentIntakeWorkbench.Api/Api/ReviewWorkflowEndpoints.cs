using AiDocumentIntakeWorkbench.Api.Review;

namespace AiDocumentIntakeWorkbench.Api.Api;

public static class ReviewWorkflowEndpoints
{
    public static IEndpointRouteBuilder MapReviewWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/intake-documents/{id:guid}/review/fields", UpdateFieldsAsync);
        app.MapPost("/api/intake-documents/{id:guid}/review/decision", RecordDecisionAsync);

        return app;
    }

    public static async Task<IResult> UpdateFieldsAsync(
        Guid id,
        ReviewFieldUpdatesRequest request,
        ReviewWorkflowService reviewWorkflowService,
        CancellationToken cancellationToken)
    {
        var result = await reviewWorkflowService.UpdateFieldsAsync(
            id,
            request.FieldUpdates
                ?.Select(update => new ReviewerFieldUpdate(
                    update.FieldName ?? string.Empty,
                    update.ReviewedValue ?? string.Empty))
                .ToArray() ?? [],
            cancellationToken);

        return ToResult(result);
    }

    public static async Task<IResult> RecordDecisionAsync(
        Guid id,
        ReviewerDecisionRequest request,
        ReviewWorkflowService reviewWorkflowService,
        CancellationToken cancellationToken)
    {
        var result = await reviewWorkflowService.RecordDecisionAsync(
            id,
            request.Decision,
            cancellationToken);

        return ToResult(result);
    }

    private static IResult ToResult(ReviewWorkflowResult result)
    {
        if (result.Succeeded && result.Response is not null)
        {
            return TypedResults.Ok(result.Response);
        }

        var message = result.ErrorMessage ?? "Review workflow action could not be completed.";

        return result.Error switch
        {
            ReviewWorkflowError.DocumentNotFound => TypedResults.NotFound(new ErrorResponse(message)),
            ReviewWorkflowError.InvalidFieldUpdate => TypedResults.BadRequest(new ErrorResponse(message)),
            ReviewWorkflowError.UnknownField => TypedResults.BadRequest(new ErrorResponse(message)),
            ReviewWorkflowError.InvalidDecision => TypedResults.BadRequest(new ErrorResponse(message)),
            ReviewWorkflowError.NotReviewable => TypedResults.Conflict(new ErrorResponse(message)),
            ReviewWorkflowError.AlreadyFinalized => TypedResults.Conflict(new ErrorResponse(message)),
            _ => TypedResults.BadRequest(new ErrorResponse(message))
        };
    }
}

public sealed record ReviewFieldUpdatesRequest(
    IReadOnlyList<ReviewFieldUpdateRequest>? FieldUpdates);

public sealed record ReviewFieldUpdateRequest(
    string? FieldName,
    string? ReviewedValue);

public sealed record ReviewerDecisionRequest(
    string? Decision);
