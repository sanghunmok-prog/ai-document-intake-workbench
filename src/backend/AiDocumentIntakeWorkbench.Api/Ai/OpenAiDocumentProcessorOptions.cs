namespace AiDocumentIntakeWorkbench.Api.Ai;

public sealed class OpenAiDocumentProcessorOptions
{
    public const string SectionName = "OpenAI";

    public string? ApiKey { get; set; }

    public string? Model { get; set; }

    public string Endpoint { get; set; } = "https://api.openai.com/v1/responses";
}
