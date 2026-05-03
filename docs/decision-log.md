# Decision Log

## 2026-04-30
- Keep heavy deploy flow as manual dispatch only.
- Add separate lightweight CI workflow for quick feedback on PR/push.
- Limit refactor to naming/comment/structure-safe updates to avoid behavior changes.

## 2026-05-01
- Keep current page-based split behavior as baseline for speed-critical workflows.
- Introduce dual-mode direction: Fast Split default (no embedding/LLM) and Smart RAG optional post-process.
- Use lazy/background embedding plus hash-based cache keys to avoid repeated token usage on unchanged files.

## 2026-05-02
- Keep existing page-based PDF outputs unchanged and add JSONL chunk export as an optional artifact in `output_split`.
- Use iText text extraction already available in the project rather than adding a new PDF parsing dependency.
- Store both parent and child chunk records in one JSONL file so downstream RAG indexing can load precise child matches and expand to parent context.
- Treat hybrid BM25/vector/rerank as a downstream retrieval concern for now; include retrieval policy metadata instead of adding a search service to the desktop splitter.
- Keep Fast Split as the default user-visible behavior by requiring the `RAG JSONL` option for chunk export.
- Use `source_file_sha256 + export_page_start + export_page_end + chunk_policy_version` to skip unchanged JSONL exports.
- Add a separate manual single-file EXE release workflow instead of changing the existing copy-deploy workflow, because the new workflow releases from this repository and does not need a PAT or target repository clone.
- Read the Syncfusion license from `SYNCFUSION_KEY` when available, and write it to `license.config` before release builds.
- Harden `copy-deploy.yml` by requiring `PAT_TOKEN` and `SYNCFUSION_KEY`, removing the hard-coded license key, and excluding generated license files from target repository copy operations.

## 2026-05-03
- Mirror the source repository's GitHub latest release EXE to the target repository release using the same tag, release title, notes, prerelease flag, and asset instead of creating a timestamped target release from a fresh `copy-deploy` build.
- Keep source-file synchronization in `copy-deploy.yml`, but remove the in-workflow .NET publish requirement because the deployable EXE now comes from the already-published source release.
- Pin GitHub Actions and `global.json` to .NET SDK `9.0.312` with `latestFeature` roll-forward because the project targets `net9.0-windows` and GitHub-hosted Windows runners may not have local developer patch SDK `9.0.313`.
- Change `copy-deploy.yml` to build the single-file EXE directly from the requested tag/branch and upload it to the target repository release. This avoids depending on a source repository `latest release`, which can return HTTP 404 when no source release exists or when token access is insufficient.
