namespace AiDocumentIntakeWorkbench.Api.Ai;

public static class OpenAiDocumentPrompt
{
    public const string SystemInstructions = """
        You classify inbound business documents and extract structured fields for a human review workflow.
        Return only JSON matching the requested schema.
        Do not approve, reject, finalize, call external systems, send messages, or make final business decisions.
        Suggested routing may describe human review needs, but final workflow decisions belong to human reviewers.
        """;

    public static readonly object OutputSchema = new
    {
        type = "object",
        additionalProperties = false,
        required = new[]
        {
            "sourceSampleDocumentId",
            "documentType",
            "overallConfidence",
            "extractedFields",
            "missingFields",
            "riskIndicators",
            "suggestedRouting",
            "rationale"
        },
        properties = new
        {
            sourceSampleDocumentId = new { type = "string" },
            documentType = new { type = "string" },
            overallConfidence = new { type = "number", minimum = 0, maximum = 1 },
            extractedFields = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    additionalProperties = false,
                    required = new[] { "name", "value", "confidence" },
                    properties = new
                    {
                        name = new { type = "string" },
                        value = new { type = "string" },
                        confidence = new { type = "number", minimum = 0, maximum = 1 }
                    }
                }
            },
            missingFields = new
            {
                type = "array",
                items = new { type = "string" }
            },
            riskIndicators = new
            {
                type = "array",
                items = new { type = "string" }
            },
            suggestedRouting = new { type = "string" },
            rationale = new { type = "string" }
        }
    };

    public static string CreateUserInput(OpenAiStructuredOutputRequest request)
    {
        return $"""
            Sample document ID: {request.SampleDocumentId}
            Scenario: {request.Scenario ?? "Not provided"}
            Display name: {request.DisplayName}
            Summary: {request.Summary ?? "Not provided"}

            Document content:
            {request.Content ?? "Not provided"}
            """;
    }
}
