using AiDocumentIntakeWorkbench.Api.Domain;

namespace AiDocumentIntakeWorkbench.Api.Intake;

public enum CreateIntakeDocumentError
{
    MissingSampleDocumentId,
    SampleDocumentNotFound
}

public sealed record CreateIntakeDocumentResult(
    IntakeDocument? IntakeDocument,
    CreateIntakeDocumentError? Error)
{
    public bool Succeeded => IntakeDocument is not null && Error is null;

    public static CreateIntakeDocumentResult Success(IntakeDocument intakeDocument)
    {
        return new CreateIntakeDocumentResult(intakeDocument, null);
    }

    public static CreateIntakeDocumentResult Failure(CreateIntakeDocumentError error)
    {
        return new CreateIntakeDocumentResult(null, error);
    }
}
