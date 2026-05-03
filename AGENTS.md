# Agent Instructions

## Communication
- Explain decisions, questions, and progress in Korean.
- Keep code blocks, commands, file contents, API names, package names, and original quoted text in English.
- For business automation tasks, explain user-visible behavior and operational impact clearly.

## Source Of Truth
- Implementation plan: `docs/implementation-plan.md`
- Progress tracker: `docs/progress-tracker.md`
- Verification log: `docs/test-log.md`
- Agent workflow: `docs/codex-workflow.md`
- Architecture and operational decisions: `docs/decision-log.md`
- Business process notes: `docs/business-process.md`
- Do not create duplicate planning, tracker, workflow, or decision documents.
- If these documents do not exist, rely on `README.md`, existing issue descriptions, and current repo structure.

## Workflow
- Do not develop directly on `main` or `master`.
- Default working branch: `work/automation-implementation`
- Start every task by running `git status --short --branch`.
- If on `main` or `master`, switch to the default working branch or create a task-specific branch.
- Review relevant docs before changing code.
- Update `docs/progress-tracker.md` to `In Progress` before implementation when the file exists.
- Implement one logical change at a time.
- Avoid unrelated refactoring, formatting, dependency upgrades, and broad rewrites.
- After implementation, record actual commands and outcomes in `docs/test-log.md` when the file exists.
- Mark the task as `Done` or `Blocked` in `docs/progress-tracker.md` when the file exists.
- Commit completed work if the repo workflow expects commits.
- Do not push unless explicitly requested or required by the active workflow.

## Project Scope
- This repo may contain one or more of the following:
  - Microsoft Office Add-ins
  - Excel automation
  - Microsoft Graph integrations
  - Power Platform or Power Automate support files
  - .NET services, APIs, workers, or console tools
  - Python automation scripts
  - JavaScript or TypeScript tooling
- Stay within the task scope.
- Preserve existing business rules, workbook structures, API contracts, and automation behavior.
- Do not change production workflows, tenant settings, environment-specific configuration, or deployment behavior unless explicitly required.

## Engineering Rules
- Prefer existing language, framework, architecture, and folder structure.
- Use TypeScript over JavaScript when the repo already uses TypeScript.
- Use existing .NET project conventions, dependency injection patterns, logging patterns, and configuration patterns.
- Use existing Python environment and packaging conventions.
- Keep Excel workbook assumptions explicit, including worksheet names, table names, named ranges, columns, and expected data types.
- For Office Add-ins, preserve manifest compatibility and host requirements.
- For Power Platform assets, avoid manual edits to generated solution files unless the repo convention requires it.
- Do not introduce new dependencies unless the benefit is clear and the dependency is appropriate for the repo.
- Do not commit secrets, tenant IDs that are not already public config, client secrets, certificates, connection strings, refresh tokens, service accounts, signing keys, or production credentials.

## Build/Test Commands
- Inspect the repo before choosing commands.
- JavaScript/TypeScript:
  - Dependencies: `npm ci`
  - Lint: `npm run lint`
  - Build: `npm run build`
  - Tests: `npm test`
- Office Add-ins:
  - Validate manifest when supported: `npm run validate`
  - Start local add-in when supported: `npm start`
  - Build production bundle when supported: `npm run build`
- .NET:
  - Restore: `dotnet restore`
  - Build: `dotnet build`
  - Test: `dotnet test`
  - Format check when configured: `dotnet format --verify-no-changes`
- Python:
  - Install dependencies according to repo convention.
  - Tests: `pytest`
  - Lint: `ruff check .`
  - Type check when configured: `mypy .` or `pyright`
- Power Platform:
  - Validate solution export/import scripts when present.
  - Do not run tenant-impacting commands unless explicitly requested.
- General:
  - Whitespace check: `git diff --check`
- Run only commands relevant to the changed files.
- If a command is unavailable or fails due to environment limitations, record the reason and the observed output.

## Testing Strategy
- Business rules, calculations, parsing, mapping, transformation, and validation logic require tests.
- Excel automation should test workbook-independent logic separately from Excel host integration.
- Office Add-in changes should include tests for task pane logic, command behavior, or state management when supported.
- .NET service, repository, API, worker, and integration logic should have unit tests or focused integration tests.
- Python automation should test pure functions, file parsing, data conversion, and error handling.
- Bug fixes should include a reproduction test before the fix when practical.
- For UI-only or manifest-only changes, run a smoke test or validation command.
- For authentication, authorization, tenant, permission, export, deletion, and data update flows, include extra verification.
- Documentation-only changes should run `git diff --check`.

## TDD Defaults
- New business logic: write a failing test first.
- Bug fix: reproduce with a failing test or documented manual case first.
- External API integrations should be wrapped behind testable clients or adapters.
- Excel, Office.js, Graph, Dataverse, SharePoint, and Power Platform dependencies should be isolated from pure logic.
- Use mocks, fakes, fixtures, or sample workbooks where possible.
- Do not rely on production tenants, production workbooks, or production credentials for tests.

## Office Add-ins Rules
- Keep Office.js host compatibility in mind.
- Avoid unsupported browser or runtime APIs unless the manifest and target hosts allow them.
- Preserve manifest IDs, permissions, requirement sets, and source locations unless explicitly changing deployment configuration.
- Do not hard-code tenant-specific or user-specific URLs.
- Keep add-in commands, task pane routes, and shared runtime behavior consistent with existing patterns.
- Validate user-facing text for clarity, especially when automation modifies workbook data.

## Excel Automation Rules
- Treat workbook schema as a contract.
- Document assumptions about:
  - Worksheet names
  - Table names
  - Named ranges
  - Header names
  - Date, number, and currency formats
  - Locale-sensitive parsing
- Do not silently overwrite user data.
- For destructive changes, require confirmation or a clearly documented safeguard.
- Prefer idempotent automation when possible.
- Include error messages that help non-developer users recover.

## .NET Rules
- Follow existing solution and project structure.
- Prefer dependency injection for services and external clients.
- Use typed options for configuration when the repo already uses them.
- Use structured logging where available.
- Do not log secrets, tokens, connection strings, or personal data.
- Keep async code async; avoid blocking calls such as `.Result` or `.Wait()` unless justified.
- Preserve nullable reference type conventions.

## Python Rules
- Keep scripts deterministic and safe for repeated execution.
- Separate CLI/file I/O from pure transformation logic.
- Prefer explicit paths, input validation, and clear error handling.
- Do not assume the current working directory unless the repo convention requires it.
- Avoid writing generated output outside the expected output directory.

## JavaScript/TypeScript Rules
- Follow existing lint, formatting, module, and bundling conventions.
- Prefer typed interfaces for external data and API responses.
- Avoid global mutable state unless required by Office.js or existing architecture.
- Keep browser, Node.js, and Office runtime boundaries clear.
- Do not weaken type checks to bypass errors.

## Security Rules
- Never trust client input for identity, ownership, role, tenant, workbook, or permission boundaries.
- Validate authorization in trusted backend code whenever applicable.
- Do not expose data outside the allowed user, group, tenant, or business scope.
- Treat exports, imports, deletions, permission changes, tenant operations, and credential changes as sensitive actions.
- Use audit logging for sensitive actions when supported.
- Do not log confidential workbook data, customer data, personal data, access tokens, refresh tokens, or secrets.
- Keep secrets in environment variables, secret stores, local ignored files, or platform-managed configuration.

## Documentation Rules
- Update setup docs when commands, dependencies, manifests, environment variables, or deployment steps change.
- Update business-process documentation when automation behavior changes.
- Record exact verification commands and results.
- Do not create duplicate docs when an existing source-of-truth document can be updated.
- When completed or obsolete entries are removed, compressed, or rewritten from source-of-truth docs, append the removed content to `docs/history-log.md` before deleting it from the original file.
- `docs/history-log.md` is append-only. Each archive entry must include date, source file path, reason for archiving, and the removed original content or a faithful excerpt when the content is too large.
- Never silently delete completed records from `docs/implementation-plan.md`, `docs/progress-tracker.md`, `docs/test-log.md`, `docs/decision-log.md`, or other source-of-truth docs.

## Completion Criteria
- The requested change is implemented within scope.
- Relevant build, lint, test, validation, or smoke commands were run.
- Verification results are documented when the repo uses `docs/test-log.md`.
- Progress is updated when the repo uses `docs/progress-tracker.md`.
- `git status --short` is reviewed before final reporting.
- The final response includes:
  - Changed files
  - What changed
  - Verification commands and results
  - Known limitations or follow-up items
