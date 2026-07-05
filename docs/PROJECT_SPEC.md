# Project Specification

## Product Purpose

AI Document Intake Workbench is a full-stack enterprise-style workflow application for document intake, AI-assisted triage, validation, human review, workflow status tracking, and audit history.

The MVP focuses on structured assistance. AI may classify documents and extract fields, but backend validation and human review determine final workflow outcomes.

## Core Workflow

1. Intake or select a sample document.
2. Run AI-assisted document classification and structured field extraction.
3. Validate the structured result on the backend.
4. Generate validation flags for missing, low-confidence, conflicting, or inconsistent data.
5. Place the item in a human review queue.
6. Show review detail with the source document context, extracted fields, validation flags, workflow status, and audit history.
7. Allow a reviewer to edit fields and make the final decision.
8. Record audit events for material workflow and review activity.

## User And Workflow Roles

- Intake user: Adds or selects documents for processing.
- Reviewer: Reviews AI-assisted output, edits fields when needed, and makes final decisions.
- System and AI processor: Produces structured assistance, runs validation, updates workflow status, and writes audit events according to backend rules.

## Functional MVP Requirements

- Sample document scenarios that demonstrate the intake workflow locally.
- Structured AI output for classification and field extraction.
- Backend validation that does not trust AI output as final state.
- Validation flags for missing data, low confidence, conflicting values, and inconsistent values.
- Human review queue for documents needing review.
- Review detail view for extracted fields, validation flags, status, and audit history.
- Reviewer decisions that finalize the reviewed outcome.
- Audit trail for important intake, processing, validation, review, and decision events.
- Simple workflow statuses that make progress visible.
- Local demo support without external AI keys when implementation exists.

## AI Integration Boundary

- Use an AI service abstraction before adding provider-specific code.
- Implement deterministic mock AI before any real provider.
- Require structured AI output.
- Validate AI output before it affects review state.
- Do not let AI approve, reject, finalize, directly mutate final business state, modify audit history, call third-party systems, send messages, or behave like a general assistant.
- Do not build a chat UI.

## Data Concepts

- Intake document: A document or sample input submitted to the workflow.
- AI processing result: A structured processing output associated with an intake document.
- Extracted fields: Named values produced by AI-assisted extraction and later reviewed by a human.
- Validation flags: Backend-generated issues or warnings that explain review concerns.
- Review state: Current workflow status, assigned review context, reviewer edits, and final reviewer decision.
- Audit event: Append-only history entry for important workflow activity.

## Backend Expectations

- Use ASP.NET Core Web API with C#.
- Use Entity Framework Core and SQL Server for persistence when backend implementation begins.
- Expose REST APIs for the frontend workflow.
- Keep service boundaries clear for intake, AI processing, validation, review, and audit writing.
- Use backend validation for AI output and reviewer updates.
- Write audit events from backend workflow operations.
- Use environment-based configuration.
- Do not hardcode secrets.

## Frontend Expectations

- Use Angular and TypeScript when frontend implementation begins.
- Build workflow screens rather than a chat interface.
- Show structured results, validation flags, review status, and audit history.
- Include loading, error, and empty states for workflow screens.
- Keep UI behavior aligned with human review as the final decision point.

## Technical Constraints

- Keep dependencies minimal.
- Avoid unnecessary architecture.
- Do not introduce microservices, cloud infrastructure, or Kubernetes.
- Do not hardcode secrets.
- Prefer simple, local development support before optional external services.

## Explicitly Out Of Scope

- Chatbot.
- ChatGPT clone.
- Chat with PDFs.
- Broad RAG knowledge base.
- OCR platform.
- Full document-management system.
- Full claims or case-management system.
- Multi-agent automation.
- Model training or fine-tuning.
- Vector database.
- Cloud infrastructure.
- Kubernetes.
- Microservices.
- External integrations.
- Autonomous approvals or rejections.
