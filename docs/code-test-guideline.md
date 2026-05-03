# Code Test Guideline

## Scope
This guideline defines minimum checks for changes in this repository.

## Baseline Checks
- Always run `git diff --check` before commit.
- For documentation-only changes, `git diff --check` is required.

## .NET Checks (when C# or project files change)
1. `dotnet restore PDFSplitterforCopilot.sln`
2. `dotnet build PDFSplitterforCopilot.sln -c Release`
3. `dotnet test` (if test projects are available and runnable)

## CI Expectations
- PRs should pass the lightweight validation workflow.
- Expensive publish/deploy steps must remain manual (`workflow_dispatch`).

## Test Log Practice
- Record command, result, and short note in `docs/test-log.md`.
