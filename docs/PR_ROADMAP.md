# PR Roadmap

No future PR should begin until the current PR has been manually reviewed and accepted.

## PR00 - Public-Safe Repository Foundation And Documentation

Goal: Establish public-safe repository documentation for future work.

Scope: README, ignore rules, agent instructions, project specification, roadmap, and testing guidance.

Explicitly out of scope: Product behavior, app scaffolding, dependencies, generated files, backend code, frontend code, tests, sample data, and provider integrations.

Validation expectations: Confirm only allowed documentation files changed, no product code exists, no forbidden terms are present, and no build commands are required.

Stop condition: Public-safe documentation is complete and unstaged for manual review.

## PR01 - Backend And Frontend Shell

Goal: Create minimal backend and frontend application shells.

Scope: Add the ASP.NET Core Web API shell and Angular shell with local startup documentation.

Explicitly out of scope: Domain persistence, AI behavior, document intake features, review workflows, and audit features.

Validation expectations: Restore, build, and run the new shells using the commands documented in that PR.

Stop condition: Empty shells run locally and no MVP feature behavior is implemented.

## PR02 - Core Domain And Persistence Foundation

Goal: Add the initial domain model and persistence foundation.

Scope: Introduce core entities, database context, configuration, and basic persistence wiring for the planned workflow.

Explicitly out of scope: AI processing, intake UI, review UI, reviewer decisions, and real provider integration.

Validation expectations: Backend build and tests cover the persistence foundation and basic domain rules.

Stop condition: Domain and persistence basics exist without implementing end-to-end workflow features.

## PR03 - Sample Document Intake Without AI

Goal: Add local sample document intake without AI processing.

Scope: Add a simple intake flow for local sample documents and persist the initial workflow state.

Explicitly out of scope: AI classification, extraction, validation flags, review decisions, and provider integration.

Validation expectations: Backend and frontend validation demonstrate sample intake and persistence.

Stop condition: Sample intake works locally without AI behavior.

## PR04 - AI Service Interface And Deterministic Mock AI

Goal: Add the AI abstraction and deterministic mock processor.

Scope: Define the AI service boundary and implement deterministic structured mock output for known sample scenarios.

Explicitly out of scope: Real AI providers, external calls, chat UI, autonomous decisions, and final state mutation by AI.

Validation expectations: Tests confirm deterministic structured output for repeated runs and expected sample scenarios.

Stop condition: Mock AI output is available behind the service boundary with no real provider dependency.

## PR05 - AI Processing Workflow And Validation Flags

Goal: Connect mock AI processing to backend validation and workflow status.

Scope: Store AI processing results, run validation, create validation flags, and update workflow status.

Explicitly out of scope: Reviewer edit workflow, final reviewer decisions, real AI providers, and external integrations.

Validation expectations: Tests cover clean, missing, low-confidence, conflicting, and inconsistent mock outputs.

Stop condition: AI-assisted processing creates validated review-ready work items without final decisions.

## PR06 - Review Queue UI

Goal: Add a workflow-oriented review queue.

Scope: Show documents awaiting review with key status, classification, and validation flag summary.

Explicitly out of scope: Review detail editing, final decisions, audit hardening, and real provider integration.

Validation expectations: Frontend and backend validation confirm queue loading, empty state, error state, and basic filtering if included.

Stop condition: Reviewers can see work items awaiting review but cannot finalize them.

## PR07 - Review Detail Screen With Extracted Fields, Flags, And Audit History

Goal: Add review detail visibility.

Scope: Display document context, extracted fields, validation flags, workflow status, and audit history.

Explicitly out of scope: Reviewer edits, final decisions, real provider integration, and external systems.

Validation expectations: Tests and manual checks confirm structured display, loading state, empty state, and error state.

Stop condition: Reviewers can inspect a work item but cannot finalize it.

## PR08 - Reviewer Edit And Decision Workflow

Goal: Allow human reviewers to edit extracted fields and make final decisions.

Scope: Add reviewer field edits, final decision actions, status updates, validation, and audit events.

Explicitly out of scope: AI final decisions, external calls, real provider integration, and unrelated workflow expansion.

Validation expectations: Tests cover valid decisions, rejected invalid updates, audit events, and status transitions.

Stop condition: Human reviewers can finalize outcomes through the intended workflow.

## PR09 - Audit Trail Hardening And Workflow Reliability Tests

Goal: Strengthen auditability and workflow reliability.

Scope: Add targeted tests and safeguards for audit event writing, status transitions, and important edge cases.

Explicitly out of scope: New major features, real provider integration, external systems, and architecture expansion.

Validation expectations: Backend tests cover audit consistency, invalid transitions, repeat operations, and failure paths.

Stop condition: Reliability gaps in the existing workflow are covered without changing MVP scope.

## PR10 - Optional Real AI Provider Behind Existing Interface

Goal: Add an optional real AI provider behind the existing AI service abstraction.

Scope: Add provider configuration, structured output parsing, error handling, and documentation while keeping mock mode available.

Explicitly out of scope: Provider-specific behavior outside the abstraction, autonomous final decisions, chat UI, training, and external workflow integrations.

Validation expectations: Mock-mode tests remain deterministic, provider mode is configuration-gated, and local runs do not require external AI keys.

Stop condition: Optional provider support exists without changing the structured assistive AI boundary.

## PR11 - UI Readability Polish

Goal: Improve Angular UI readability and workflow presentation.

Scope: Polish Overview, Sample Intake, Review Queue, and Review Detail presentation without changing app behavior.

Explicitly out of scope: Backend behavior changes, API changes, new product features, new dependencies, and final README/demo documentation polish.

Validation expectations: Backend and frontend builds/tests pass, and existing workflow screens remain functional with improved readability.

Stop condition: Existing workflow screens are easier to scan and demo without expanding MVP scope.

## PR12 - UI Process Action For Intake Documents

Goal: Complete the UI click-through path by exposing the existing AI processing action from Angular.

Scope: Add a frontend process action for processable intake documents using the existing backend process endpoint.

Explicitly out of scope: Backend behavior changes, API changes, new routes/pages, workflow rule changes, and dependency changes.

Validation expectations: Backend and frontend builds/tests pass, and the UI path works from Sample Intake to Review Queue and Review Detail.

Stop condition: Users can create an intake document, process it with mock AI, and continue through review from the UI.

## PR13 - Final README And Demo Instructions Polish

Goal: Make the public repository demo-ready with final README and validation instructions.

Scope: Update public documentation for setup, configuration, mock-mode demo, optional provider notes, limitations, and final validation.

Explicitly out of scope: Source code changes, dependency changes, deployment files, generated media, and new product behavior.

Validation expectations: Documentation-only changes, full backend/frontend validation commands pass, and public-safety checks find no secrets or private content.

Stop condition: README clearly explains how to understand, run, test, and demo the completed MVP.
