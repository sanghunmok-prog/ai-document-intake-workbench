using AiDocumentIntakeWorkbench.Api.Ai;
using AiDocumentIntakeWorkbench.Api.Domain;

namespace AiDocumentIntakeWorkbench.Api.AiProcessing;

public static class AiProcessingValidation
{
    private const decimal LowOverallConfidenceThreshold = 0.75m;
    private const decimal LowFieldConfidenceThreshold = 0.75m;

    public static IReadOnlyList<ValidationFlag> CreateFlags(
        Guid intakeDocumentId,
        Guid documentProcessingResultId,
        DocumentAiOutput output)
    {
        var flags = new List<ValidationFlag>();

        if (string.IsNullOrWhiteSpace(output.DocumentType))
        {
            flags.Add(new ValidationFlag(
                intakeDocumentId,
                documentProcessingResultId,
                ValidationFlagType.IncompleteStructuredOutput,
                ValidationFlagSeverity.Error,
                "AI output did not include a document type."));
        }

        if (output.Fields.Count == 0)
        {
            flags.Add(new ValidationFlag(
                intakeDocumentId,
                documentProcessingResultId,
                ValidationFlagType.IncompleteStructuredOutput,
                ValidationFlagSeverity.Error,
                "AI output did not include extracted fields."));
        }

        foreach (var missingField in output.MissingFields)
        {
            flags.Add(new ValidationFlag(
                intakeDocumentId,
                documentProcessingResultId,
                ValidationFlagType.MissingRequiredField,
                ValidationFlagSeverity.Error,
                $"Required field '{missingField}' is missing.",
                missingField));
        }

        if (output.OverallConfidence < LowOverallConfidenceThreshold)
        {
            flags.Add(new ValidationFlag(
                intakeDocumentId,
                documentProcessingResultId,
                ValidationFlagType.LowConfidence,
                ValidationFlagSeverity.Warning,
                $"Overall confidence {output.OverallConfidence:0.00} is below the review threshold."));
        }

        foreach (var field in output.Fields.Where(field => field.Confidence < LowFieldConfidenceThreshold))
        {
            flags.Add(new ValidationFlag(
                intakeDocumentId,
                documentProcessingResultId,
                ValidationFlagType.LowConfidence,
                ValidationFlagSeverity.Warning,
                $"Field '{field.Name}' confidence {field.Confidence:0.00} is below the review threshold.",
                field.Name));
        }

        foreach (var riskIndicator in output.RiskIndicators)
        {
            var type = IsInconsistentRisk(riskIndicator)
                ? ValidationFlagType.InconsistentData
                : ValidationFlagType.RiskIndicator;

            var severity = type == ValidationFlagType.InconsistentData
                ? ValidationFlagSeverity.Error
                : ValidationFlagSeverity.Warning;

            flags.Add(new ValidationFlag(
                intakeDocumentId,
                documentProcessingResultId,
                type,
                severity,
                riskIndicator));
        }

        return flags;
    }

    private static bool IsInconsistentRisk(string riskIndicator)
    {
        return riskIndicator.Contains("conflict", StringComparison.OrdinalIgnoreCase)
            || riskIndicator.Contains("inconsistent", StringComparison.OrdinalIgnoreCase);
    }
}
