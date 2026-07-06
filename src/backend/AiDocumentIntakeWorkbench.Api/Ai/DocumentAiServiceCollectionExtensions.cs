namespace AiDocumentIntakeWorkbench.Api.Ai;

public static class DocumentAiServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentAiProcessor(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var aiProviderOptions = new AiProviderOptions();
        configuration.GetSection(AiProviderOptions.SectionName).Bind(aiProviderOptions);

        services.Configure<AiProviderOptions>(
            configuration.GetSection(AiProviderOptions.SectionName));
        services.Configure<OpenAiDocumentProcessorOptions>(
            configuration.GetSection(OpenAiDocumentProcessorOptions.SectionName));

        if (string.Equals(aiProviderOptions.Mode, AiProviderModes.OpenAI, StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IOpenAiStructuredOutputClient, HttpOpenAiStructuredOutputClient>();
            services.AddScoped<IDocumentAiProcessor, OpenAiDocumentAiProcessor>();
            return services;
        }

        services.AddSingleton<IDocumentAiProcessor, DeterministicMockDocumentAiProcessor>();
        return services;
    }
}
