using AiDocumentIntakeWorkbench.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var workbenchDbConnectionString = builder.Configuration.GetConnectionString(WorkbenchDbContext.ConnectionStringName);
builder.Services.AddDbContext<WorkbenchDbContext>(options =>
{
    options.UseSqlServer(WorkbenchDbContext.ResolveConnectionString(workbenchDbConnectionString));
});

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
