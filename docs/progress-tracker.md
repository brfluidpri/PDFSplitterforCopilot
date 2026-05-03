# Progress Tracker

- 2026-05-03 | Rework copy-deploy to build target release EXE directly | Done
  - Step 1: Diagnose latest release 404 vs asset size | Done
  - Step 2: Add manual release-style inputs and build flow to `copy-deploy.yml` | Done
  - Step 3: Upload generated EXE directly to target repository release | Done
  - Step 4: Validate workflow scripts and record results | Done

- 2026-05-03 | Fix GitHub Actions SDK resolution for manual release | Done
  - Step 1: Identify `global.json` and workflow SDK mismatch | Done
  - Step 2: Align CI and manual release workflows with available .NET 9 SDK | Done
  - Step 3: Validate restore/build and record results | Done

- 2026-05-03 | Copy-deploy latest release EXE propagation | Done
  - Step 1: Review current copy-deploy and release workflows | Done
  - Step 2: Download latest source repository release EXE during copy-deploy | Done
  - Step 3: Create or update matching target repository release | Done
  - Step 4: Validate workflow syntax and record results | Done

- 2026-05-03 | Remove obsolete `run.bat` helper script | Done
  - Step 1: Delete outdated `run.bat` | Done
  - Step 2: Remove stale README reference | Done
  - Step 3: Validate references and whitespace | Done

- 2026-05-03 | Commercial UX Phase 1 task-centered UI and processing split cleanup | Done
  - Step 1: Record implementation plan and tracker status | Done
  - Step 2: Rework top workflow UI around task, settings, advanced export, and primary run action | Done
  - Step 3: Clarify operation-specific enabled states and helper text | Done
  - Step 4: Split first-level processing branch helpers in `MainWindow.xaml.cs` | Done
  - Step 5: Validate and record results | Done

- 2026-05-03 | Commercial UX Phases 2-6 preview, output, structure, stability, and onboarding | Done
  - Phase 2: Editable Context Split preview with validation | Done
  - Phase 3: Generated output tracking and output column | Done
  - Phase 4: Extract output path construction into `OutputFileService` | Done
  - Phase 5: Add cancel control for multi-file processing | Done
  - Phase 6: Add first-run commercial workflow notes to README | Done
  - Validation and documentation | Done

- 2026-05-03 | Context Split boundary overlap and short output file names | Done
  - Add Context Split-only overlap input | Done
  - Show semantic vs included ranges in preview | Done
  - Generate context PDFs from included ranges | Done
  - Use short `{basename}_partNN_pXX-YY.pdf` output names | Done
  - Validate and record results | Done

- 2026-05-03 | Retarget WPF app to .NET 9 SDK and net9.0-windows | Done

- 2026-05-02 | OpenAI API settings dialog and encrypted user config | Done

- 2026-05-02 | Merge split/convert/batch convert into Operation combo box | Done

- 2026-05-02 | Data-first toolbar layout conversion | Done
  - Step 1: Record layout conversion work | Done
  - Step 2: Remove large header and fixed left panel | Done
  - Step 3: Add compact command toolbar | Done
  - Step 4: Expand DataGrid as primary workspace | Done
  - Step 5: Preserve compact status/progress bar | Done
  - Step 6: Validate and document results | Done

- 2026-05-02 | Fix RAG JSONL checkbox cross-thread access during PDF processing | Done

- 2026-05-02 | WPF UI-inspired layout and UX cleanup for split workflow | Done

- 2026-05-02 | Context Split (LLM) phase 1 UI and split proposal flow | Done

- 2026-05-02 | Implementation plan completion audit | Done

- 2026-05-02 | Require Syncfusion license config and .NET 10 for GitHub Actions | Done

- 2026-05-02 | Align manual release license generation with copy-deploy workflow | Done

- 2026-05-02 | Fix manual release checkout target for LicenseConfig build | Done

- 2026-05-02 | Final copy-deploy workflow verification and hardening | Done

- 2026-05-02 | Fix GitHub Actions LicenseConfig publish failure | Done

- 2026-05-02 | Manual GitHub release workflow for single-file EXE | Done

- 2026-05-02 | RAG JSONL mode cleanup, metadata, and incremental export | Done

- 2026-05-02 | RAG-friendly semantic chunk JSONL export for PDF outputs | Done

- 2026-04-30 | AGENTS.md 추가 및 품질 가이드/CI/리팩터링/문서화 작업 | Done

- 2026-05-01 | Fast Split/Smart RAG 모드 분리 설계 및 캐시/백그라운드 처리 계획 수립 | Done
