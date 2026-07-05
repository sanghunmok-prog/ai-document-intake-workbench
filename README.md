# AI Document Intake Workbench

AI Document Intake Workbench is a planned full-stack workflow application for AI-assisted document intake, triage, validation, human review, and auditability.

## Current Status

PR01 adds minimal runnable backend and frontend application shells. Product workflow behavior has not been implemented yet.

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

These commands run only the PR01 shells. Document intake, AI processing, validation, review, and audit workflow behavior will be added in later PRs.

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
