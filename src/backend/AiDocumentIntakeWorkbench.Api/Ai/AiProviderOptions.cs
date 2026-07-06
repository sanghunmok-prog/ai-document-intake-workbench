namespace AiDocumentIntakeWorkbench.Api.Ai;

public sealed class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    public string Mode { get; set; } = AiProviderModes.Mock;
}

public static class AiProviderModes
{
    public const string Mock = "Mock";

    public const string OpenAI = "OpenAI";
}
