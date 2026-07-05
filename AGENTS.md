# Agent Instructions

These instructions apply to future agent work in this repository.

## Required Reading

Agents must read only these public-safe sources before implementation:

- `AGENTS.md`
- `docs/PROJECT_SPEC.md`
- `docs/PR_ROADMAP.md`
- `docs/TESTING.md`
- The current PR-specific prompt

Do not rely on any non-public source or undocumented context.

## Scope Control

- Implement only the requested PR.
- Stop after the requested PR is complete.
- Do not work ahead into later roadmap items.
- Do not expand the MVP beyond the accepted scope.
- Do not add unrelated files, features, dependencies, or architecture.
- Do not introduce chatbot behavior, broad RAG, OCR platform features, cloud infrastructure, multi-agent automation, or autonomous final decisions.
- Keep AI behavior structured, bounded, and assistive.
- Preserve human final review as the final decision step.
- Avoid hardcoded secrets and local-only sensitive values.
- Run only validation commands relevant to the requested PR.

## Reporting Requirements

Final reports must include:

- Files changed.
- Commands run.
- Command results.
- Tests added or updated, if any.
- Assumptions made.
- Acceptance criteria status.
- Manual verification steps.
- Confirmation that the agent did not work ahead.
