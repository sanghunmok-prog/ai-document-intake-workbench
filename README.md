# AI Document Intake Workbench

AI Document Intake Workbench is a planned full-stack workflow application for AI-assisted document intake, triage, validation, human review, and auditability.

## Current Status

PR03 adds local sample document intake without AI processing. Users can select a sample business document, persist it as an intake document, and view its initial workflow status.

## Planned Stack

- ASP.NET Core Web API
- Angular
- SQL Server
- Entity Framework Core
- REST APIs

## Local Shell Development

Prerequisites:

- .NET SDK
- Node.js and npm

Restore and build the backend solution:

```bash
dotnet restore ./AiDocumentIntakeWorkbench.sln
dotnet build ./AiDocumentIntakeWorkbench.sln --no-restore
```

Run the backend API shell:

```bash
dotnet run --project ./src/backend/AiDocumentIntakeWorkbench.Api/AiDocumentIntakeWorkbench.Api.csproj --launch-profile http
```

The health endpoint is available at `http://localhost:5080/health` when the `http` launch profile is running.

Install, build, and run the frontend shell:

```bash
cd src/frontend/ai-document-intake-workbench-web
npm install
npm run build
npm start
```

The frontend development server is available at `http://localhost:4200/` by default.

The frontend includes a sample intake screen. AI processing, validation, review, and audit history UI will be added in later PRs.

## Local Database Configuration

The backend uses the `ConnectionStrings:WorkbenchDb` configuration key for SQL Server. The committed value is a public-safe local placeholder without credentials.

Supply a real local connection string through environment configuration or .NET user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:WorkbenchDb" "<your-local-sql-server-connection-string>" --project ./src/backend/AiDocumentIntakeWorkbench.Api/AiDocumentIntakeWorkbench.Api.csproj
```

After configuring SQL Server locally, apply migrations with:

```bash
dotnet ef database update --project ./src/backend/AiDocumentIntakeWorkbench.Api/AiDocumentIntakeWorkbench.Api.csproj --startup-project ./src/backend/AiDocumentIntakeWorkbench.Api/AiDocumentIntakeWorkbench.Api.csproj
```

The `/health` endpoint does not require a database connection.

The sample catalog endpoint does not require database records, but creating and listing intake documents requires the configured SQL Server database to be available and migrated.

## Optional OpenAI Provider

Mock AI mode remains the default, and the local demo works without an OpenAI key. OpenAI mode is optional and should be enabled only through local environment configuration or .NET user secrets.

Use `AiProvider:Mode` with `OpenAI`, plus `OpenAI:ApiKey` and `OpenAI:Model`. Do not commit secrets.

```bash
dotnet user-secrets set "AiProvider:Mode" "OpenAI" --project ./src/backend/AiDocumentIntakeWorkbench.Api/AiDocumentIntakeWorkbench.Api.csproj
dotnet user-secrets set "OpenAI:ApiKey" "<your-openai-api-key>" --project ./src/backend/AiDocumentIntakeWorkbench.Api/AiDocumentIntakeWorkbench.Api.csproj
dotnet user-secrets set "OpenAI:Model" "<model-name>" --project ./src/backend/AiDocumentIntakeWorkbench.Api/AiDocumentIntakeWorkbench.Api.csproj
```

The provider returns structured classification and extraction output only. Backend validation, workflow status changes, audit writing, and final reviewer decisions remain application-owned.

## Intended Workflow

The planned MVP will route inbound business documents through a structured workflow:

1. Intake a sample document.
2. Run AI-assisted classification and structured field extraction.
3. Validate extracted data on the backend.
4. Surface validation flags for human review.
5. Let a reviewer inspect, edit, and decide the final outcome.
6. Record workflow status changes and audit history.

AI is intended to be assistive and workflow-oriented. Human review remains the final decision step.

## Out Of Scope

This project is not a chatbot, ChatGPT clone, chat with PDFs tool, broad RAG knowledge base, OCR platform, full document-management system, full claims or case-management system, multi-agent automation system, model training or fine-tuning project, vector database project, cloud infrastructure project, Kubernetes deployment, microservices architecture, external integration platform, or autonomous approval and rejection system.

## Public Documentation

- [Agent Instructions](AGENTS.md)
- [Project Specification](docs/PROJECT_SPEC.md)
- [PR Roadmap](docs/PR_ROADMAP.md)
- [Testing and Validation](docs/TESTING.md)
