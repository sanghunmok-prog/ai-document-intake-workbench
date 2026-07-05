var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        application = "AI Document Intake Workbench API",
        status = "running"
    });
});

app.Run();
