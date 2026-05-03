# Implementation Plan

## 2026-04-30 Task
1. Add repository-level AGENTS.md instructions.
2. Add code test guideline document.
3. Add lightweight CI workflow for fast validation.
4. Apply safe bulk refactor focused on maintainability-only (no behavior change).
5. Update README with quality/CI documentation links.

## 2026-05-01 Task
1. Define two processing modes: Fast Split (default) and Smart RAG (optional).
2. Add metadata contract for split outputs to support future embedding pipeline.
3. Define background queue and lazy embedding policy for performance-first workflows.
4. Define cache key strategy to prevent duplicate embedding work.
5. Document phased rollout and verification checklist without changing current split behavior.

## 2026-05-02 Task
1. Add RAG-friendly PDF text chunk export alongside existing PDF split outputs.
2. Preserve page-based PDF splitting behavior while generating JSONL records only when the RAG JSONL option is enabled.
3. Include semantic heading metadata, source page ranges, parent-child chunk links, child overlap, retrieval policy hints, source hashes, chunk hashes, and chunk policy version.
4. Skip JSONL regeneration when the source hash, page range, and chunk policy version match the existing export.
5. Verify with Release build and whitespace checks.

## 2026-05-02 Manual Release Workflow Task
1. Add a `workflow_dispatch` GitHub Actions workflow for manual single-file Windows EXE releases.
2. Publish `PDFSplitterforCopilot.csproj` as a self-contained `win-x64` single-file executable.
3. Upload the published EXE to a GitHub Release using operator-provided tag, release name, target commit, draft, and prerelease inputs.
4. Use `SYNCFUSION_KEY` repository secret when configured, without committing license material.
5. Verify workflow syntax and local Release publish behavior where possible.

## Current vs Planned Capability Matrix

| Capability | User-visible name | Current status | How users access it today | Output | Implementation note |
| --- | --- | --- | --- | --- | --- |
| Numeric PDF Split | Fast Split / page-based split | Implemented | Add PDF or Word file, enter a page count, keep `RAG JSONL` unchecked, then run split/convert | Page-based PDF files in `output_split` | This is the default workflow and does not call an LLM or embedding service. |
| RAG JSONL Export | Smart RAG export support | Implemented | Add PDF or Word file, enter a page count, check `RAG JSONL`, then run split/convert | Page-based PDF files plus `{filename}_rag_chunks.jsonl` in `output_split` | JSONL records contain parent/child chunks, page ranges, section hints, retrieval hints, source hash, chunk hash, and chunk policy version. |
| LLM Context Split | Context Split (LLM) | Implemented, phase 1 | Choose `Context Split (LLM)` in `Split mode`, run split, review the preview, then confirm | Context-named PDF files in `output_split` | Uses OpenAI API structured output to propose semantic page ranges, then reuses existing PDF page copying after user confirmation. |

## Screen Mapping

| Screen element | Current behavior | Related capability | Gap or clarification |
| --- | --- | --- | --- |
| `모드` toggle | Switches between split and convert behavior | Numeric PDF Split / Word conversion | This is not a Fast Split vs Smart RAG mode selector today. |
| `분할/변환 페이지 수` input | Controls fixed page count for split or conversion extraction | Numeric PDF Split | This input does not define semantic or LLM-based boundaries. |
| `RAG JSONL` checkbox | Adds JSONL chunk export while preserving existing PDF split behavior | RAG JSONL Export | This prepares downstream RAG data and is separate from Context Split (LLM). |
| `Run` button | Runs the selected workflow: fixed split, context split preview, conversion, or optional RAG JSONL export | Numeric PDF Split / LLM Context Split / Word conversion / optional RAG JSONL Export | Context Split now opens a preview and writes files only after user confirmation. |
| `Split mode` selector | Switches split behavior between `Fixed pages` and `Context Split (LLM)` | Numeric PDF Split / LLM Context Split | `Context Split (LLM)` requires `OPENAI_API_KEY`; conversion mode still uses existing behavior. |
| LLM API/model settings | Environment variables or local `.env` | LLM Context Split | `OPENAI_API_KEY` is required; `OPENAI_CONTEXT_SPLIT_MODEL` is optional. |
| Context split preview/confirmation | Preview dialog | LLM Context Split | Users review title, page range, reason, and confidence before output files are written. |

## LLM Context Split Implementation Plan

| Step | Work item | Expected output | Status | Verification criteria |
| --- | --- | --- | --- | --- |
| 1 | Add a visible `Context Split (LLM)` mode or option distinct from `RAG JSONL` | Users can clearly choose fixed page split or LLM context split | Done | UI text makes clear that `RAG JSONL` is separate from LLM context splitting. |
| 2 | Add LLM provider configuration | `OPENAI_API_KEY` and optional `OPENAI_CONTEXT_SPLIT_MODEL` environment variables or local `.env` | Done | No secrets are stored in git; missing configuration produces a recoverable user-facing error. |
| 3 | Reuse PDF text extraction as LLM input preparation | Ordered page text with page numbers and detected heading hints | Done | Extraction preserves page references before calling the LLM. |
| 4 | Ask the LLM to propose context split boundaries | Structured boundary proposal with start page, end page, title/reason, and confidence | Done | Invalid, overlapping, or out-of-range boundaries are rejected before file output. |
| 5 | Add preview and confirmation before writing files | User can accept or cancel proposed ranges | Done | No semantic split files are written until the user confirms. |
| 6 | Generate PDF outputs from accepted context boundaries | Context-named PDF files in `output_split` | Done | Existing page-based split behavior remains unchanged when `Fixed pages` is selected. |
| 7 | Record audit and test coverage | Build and whitespace verification notes | Done | `dotnet build` and `git diff --check` pass; verification is recorded in `docs/test-log.md`. |

## Out of Scope / Not Yet Implemented

| Item | Current status | Rationale / next phase |
| --- | --- | --- |
| Embedding generation inside the desktop app | Not implemented | Current app only exports RAG-ready JSONL; embedding should be a downstream pipeline unless explicitly added later. |
| Vector database storage | Not implemented | No vector DB dependency or tenant-specific storage target is configured. |
| Hybrid BM25/vector search | Not implemented | Retrieval is documented as downstream behavior; JSONL includes policy hints only. |
| Reranking | Not implemented | No reranker model or API integration exists in the app today. |
| Background queue for embeddings | Designed only | Kept as a future performance policy, not part of current desktop behavior. |
| Automatic LLM split without user confirmation | Not implemented | Context split should require preview/confirmation to avoid silently writing unexpected PDF outputs. |

## WPF UI-Inspired Layout and UX Cleanup

| Area | Current issue | Planned change | Status | Verification |
| --- | --- | --- | --- | --- |
| Top command area | File actions, split settings, output options, and run action are crowded into one toolbar | Separate the workflow into a compact header, a left settings panel, and a main file list area | Done | Controls remain visible at 900x600 and do not overlap |
| Operation selection | The split/convert toggle and split mode selector appear side by side without hierarchy | Group `Operation`, `Split method`, and page count in a single `Workflow` panel | Done | `Split mode` remains available only as part of the split workflow |
| Output options | `RAG JSONL` appears like a primary action | Move `RAG JSONL` under an `Output options` section | Done | Users can distinguish split behavior from export artifacts |
| Primary action | Run button is visually similar to setup controls and moves with toolbar crowding | Pin one primary `Run` button in the settings panel and keep file actions secondary | Done | Fixed page split, context split, and conversion still use the same existing event handler |
| Visual style | Custom button sizes and broken icon/emoji text make controls inconsistent | Use a WPF UI-inspired Fluent layout with consistent card spacing, text buttons, and restrained accent colors without adding a new dependency in this pass | Done | Release build passes and existing control names/events are preserved |

## Data-First Toolbar Layout Conversion

| Step | Work item | Planned change | Status | Verification |
| --- | --- | --- | --- | --- |
| 1 | Record layout conversion work | Track the data-first layout change in `docs/progress-tracker.md` before editing UI | Done | Progress tracker shows the layout conversion task and step status |
| 2 | Remove large header and fixed left panel | Drop the app title/header card and left workflow panel so the file list becomes the primary surface | Done | No large header or left settings column remains in `MainWindow.xaml` |
| 3 | Add compact command toolbar | Move file actions, operation, split method, page count, RAG JSONL, batch convert, and run action into a top toolbar | Done | Existing control names and event handlers are preserved |
| 4 | Expand `DataGrid` | Let `dgFiles` occupy the full central area between toolbar and status bar | Done | Registered files are visible across the widest available area |
| 5 | Preserve status and progress | Keep the bottom status/progress bar compact and unchanged behaviorally | Done | `txtStatus`, `progressContainer`, and `progressBar` remain wired |
| 6 | Validate and document | Run Release build and whitespace checks, then update tracker/test log | Done | `dotnet build` and `git diff --check` results are recorded |

## Commercial UX Phase 1

| Area | Previous behavior | Implemented behavior | Status | Verification |
| --- | --- | --- | --- | --- |
| Task selection | File actions, operation, split method, page count, RAG export, and run action were visually flat in one toolbar | The top workspace is grouped into file list actions, task/method settings, advanced export, and a fixed primary run action | Done | `DataGrid` remains the dominant central surface and the run action stays on the right |
| User guidance | `Operation`, `Method`, `Pages`, and `RAG JSONL` relied on labels only | Helper text explains fixed split, context split target pages, conversion, batch conversion, and advanced RAG export meaning | Done | Operation and method changes refresh helper text and labels |
| Advanced export | `RAG JSONL` looked like a peer mode beside split settings | The checkbox is labeled `Advanced export: RAG JSONL` with a tooltip explaining downstream indexing | Done | Users can distinguish split behavior from optional RAG artifacts |
| Operation-specific state | Disabled controls did not clearly explain why they changed | Convert and batch modes disable split-only controls and update helper text to reflect the active task | Done | Existing `ApplyOperationUiState` controls enabled states and text |
| Code boundary | First-level process handling mixed UI selection, validation, and processing state inline | Selection, processable-file filtering, invalid-file filtering, and progress UI state were split into dedicated helpers | Done | Release build passes without changing PDF/Word processing engines |

## Commercial UX Phases 2-6

| Phase | Goal | Implemented behavior | Status | Verification |
| --- | --- | --- | --- | --- |
| Phase 2 | Preview/Edit UX | `Context Split Preview` now allows editing title/start/end pages, shows estimated output file names, highlights low-confidence rows, and blocks create when ranges have gaps, overlaps, or out-of-range pages | Done | Release build passes |
| Phase 3 | Result/file management UX | File rows track generated output paths, expose an `Output` column, and allow users to review generated files and open the output folder | Done | Release build passes |
| Phase 4 | Code structure split | Output path construction was moved into `OutputFileService` to reduce path duplication and prepare broader service extraction | Done | Release build passes |
| Phase 5 | Stability and job control | A visible `Cancel` button appears during processing and stops multi-file workflows before the next file begins | Done | Release build passes |
| Phase 6 | Commercial onboarding | README documents first-run workflow, OpenAI settings, advanced RAG export usage, output folder behavior, and cancel behavior | Done | `git diff --check` passes |

## Context Split Boundary Overlap

| Area | Behavior | Status | Verification |
| --- | --- | --- | --- |
| Overlap control | `Context Split (LLM)` shows an `Overlap` input for 0-2 boundary pages; non-context workflows hide it | Done | Release build passes |
| Preview clarity | Preview separates semantic range from included range so users can see what the LLM proposed and what pages will actually be copied | Done | Dialog build passes |
| Output naming | Context output files use short included-page names like `{basename}_part02_p10-21.pdf` | Done | `OutputFileService` creates the new name format |
| PDF generation | Context split copies included ranges while preserving semantic ranges for review and reasoning | Done | Release build passes |

## Operation Selection Cleanup

| Area | Previous UI | New UI | Status | Verification |
| --- | --- | --- | --- | --- |
| Operation selection | Separate `Split / Convert` toggle plus `Batch convert` checkbox | One `Operation` ComboBox with `Split PDF`, `Convert Word`, and `Batch Convert Word` | Done | Selected operation drives the same existing processing branches |
| Operation-specific controls | Batch convert could look like an output option | Batch conversion is an operation; split method, pages, and RAG JSONL are disabled when not applicable | Done | Release build passes |

## OpenAI API Settings UX

| Area | Previous behavior | Implemented behavior | Status | Verification |
| --- | --- | --- | --- | --- |
| Settings entry point | Users had to configure environment variables or `.env` manually | `Options > OpenAI API Settings...` opens an API key/model settings dialog | Done | Menu item is wired to the dialog |
| Secret storage | `.env` was the only local file fallback | API key is saved under `%AppData%\PDFSplitterforCopilot\openai-settings.json` encrypted with Windows current-user protection | Done | No repository secret is written |
| Model setting | `OPENAI_CONTEXT_SPLIT_MODEL` environment or `.env` only | Model can be saved in app settings and defaults to `gpt-4o-mini` | Done | `ContextSplitService` reads through `OpenAISettingsService` |
| Missing API key flow | Context Split failed with a configuration error | Context Split opens the settings dialog automatically before creating any files | Done | Canceling settings returns before processing |
