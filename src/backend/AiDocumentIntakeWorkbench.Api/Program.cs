using AiDocumentIntakeWorkbench.Api.Api;
using AiDocumentIntakeWorkbench.Api.Ai;
using AiDocumentIntakeWorkbench.Api.AiProcessing;
using AiDocumentIntakeWorkbench.Api.Data;
using AiDocumentIntakeWorkbench.Api.Intake;
using AiDocumentIntakeWorkbench.Api.SampleDocuments;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var workbenchDbConnectionString = builder.Configuration.GetConnectionString(WorkbenchDbContext.ConnectionStringName);
builder.Services.AddDbContext<WorkbenchDbContext>(options =>
{
    options.UseSqlServer(WorkbenchDbContext.ResolveConnectionString(workbenchDbConnectionString));
});
builder.Services.AddSingleton<ISampleDocumentCatalog, InMemorySampleDocumentCatalog>();
builder.Services.AddSingleton<IDocumentAiProcessor, DeterministicMockDocumentAiProcessor>();
builder.Services.AddScoped<IntakeDocumentService>();
builder.Services.AddScoped<AiProcessingService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("LocalFrontend");

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        application = "AI Document Intake Workbench API",
        status = "running"
    });
});
app.MapSampleDocumentEndpoints();
app.MapIntakeDocumentEndpoints();
app.MapAiProcessingEndpoints();
app.MapReviewQueueEndpoints();

app.Run();
