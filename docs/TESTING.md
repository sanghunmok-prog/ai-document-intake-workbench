# Testing And Validation

## PR00 Validation

PR00 is documentation-only. Validation should inspect the working tree and public documentation.

- Check that only allowed files changed.
- Confirm public-safety terms do not appear in tracked documentation.
- Confirm no product code was added.
- Do not run dotnet or npm build commands because no buildable projects should exist yet.

## Future Backend Commands

When backend projects exist, use bash-compatible commands such as:

```bash
dotnet restore
dotnet build
dotnet test
```

When migrations exist, run the appropriate Entity Framework Core migration or database commands documented by that PR.

## Future Frontend Commands

When frontend projects exist, use the package install command selected by that PR, then run configured validation commands such as:

```bash
npm run build
npm test
```

Run the configured lint command if one exists.

## Mock AI Test Expectations

Mock AI tests should cover:

- Clean, high-confidence structured output.
- Missing or low-confidence fields.
- Conflicting or inconsistent values.
- Deterministic repeated output for the same input.
- Structured output that can be validated without provider-specific behavior.

## Manual Smoke Tests

After each implementation PR, manually verify the main workflow path introduced by that PR. Include loading, error, and empty states when a UI exists, and confirm local runs do not require external AI keys unless an optional provider mode is explicitly enabled.

## Final MVP Validation

Use mock AI as the default local validation path. A final local check should cover:

- Create a sample intake document from the Angular Sample Intake page.
- Process it with deterministic mock AI from the UI.
- Confirm the persisted intake document moves into the review-ready workflow state.
- Open the Review Queue and then Review Detail.
- Confirm source context, AI assessment, extracted fields, validation flags, reviewer decision controls, and audit history are visible.
- Save a reviewed field edit and record a final reviewer decision.

Automated tests and the documented local demo do not require OpenAI API keys. Optional live provider testing, when explicitly configured, should remain manual and should still verify backend validation and human review behavior.

## General Safety Checks

- No hardcoded secrets.
- No unnecessary dependencies.
- No scope creep beyond the requested PR.
- No chatbot behavior.
- No work-ahead into later roadmap items.
