using AiDocumentIntakeWorkbench.Api.Domain;
using AiDocumentIntakeWorkbench.Api.Intake;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AiDocumentIntakeWorkbench.Api.Api;

public static class IntakeDocumentEndpoints
{
    public static IEndpointRouteBuilder MapIntakeDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/intake-documents", ListAsync);
        app.MapPost("/api/intake-documents/from-sample", CreateFromSampleAsync);

        return app;
    }

    public static async Task<Ok<IntakeDocumentResponse[]>> ListAsync(
        IntakeDocumentService intakeDocumentService,
        CancellationToken cancellationToken)
    {
        var intakeDocuments = await intakeDocumentService.ListAsync(cancellationToken);
        var response = intakeDocuments
            .Select(IntakeDocumentResponse.From)
            .ToArray();

        return TypedResults.Ok(response);
    }

    public static async Task<IResult> CreateFromSampleAsync(
        CreateIntakeDocumentFromSampleRequest request,
        IntakeDocumentService intakeDocumentService,
        CancellationToken cancellationToken)
    {
        var result = await intakeDocumentService.CreateFromSampleAsync(
            request.SampleDocumentId,
            cancellationToken);

        if (result.Succeeded && result.IntakeDocument is not null)
        {
            return TypedResults.Created(
                $"/api/intake-documents/{result.IntakeDocument.Id}",
                IntakeDocumentResponse.From(result.IntakeDocument));
        }

        return result.Error switch
        {
            CreateIntakeDocumentError.MissingSampleDocumentId => TypedResults.BadRequest(
                new ErrorResponse("sampleDocumentId is required.")),
            CreateIntakeDocumentError.SampleDocumentNotFound => TypedResults.NotFound(
                new ErrorResponse("Sample document was not found.")),
            _ => TypedResults.BadRequest(new ErrorResponse("Sample document intake could not be created."))
        };
    }
}

public sealed record CreateIntakeDocumentFromSampleRequest(string? SampleDocumentId);

public sealed record IntakeDocumentResponse(
    Guid Id,
    string DisplayName,
    string Status,
    string? SampleDocumentId,
    string? Scenario,
    DateTime CreatedUtc,
    DateTime UpdatedUtc)
{
    public static IntakeDocumentResponse From(IntakeDocument intakeDocument)
    {
        return new IntakeDocumentResponse(
            intakeDocument.Id,
            intakeDocument.DisplayName,
            intakeDocument.Status.ToString(),
            intakeDocument.SampleDocumentId,
            intakeDocument.Scenario,
            intakeDocument.CreatedUtc,
            intakeDocument.UpdatedUtc);
    }
}

public sealed record ErrorResponse(string Message);
