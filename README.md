# AI Document Intake Workbench

AI Document Intake Workbench is a planned full-stack workflow application for AI-assisted document intake, triage, validation, human review, and auditability.

## Current Status

PR00 establishes the public-safe documentation foundation only. Product implementation has not started, and no backend or frontend projects exist yet.

Local run instructions will be added after backend and frontend projects are created in later PRs.

## Planned Stack

- ASP.NET Core Web API
- Angular
- SQL Server
- Entity Framework Core
- REST APIs

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
