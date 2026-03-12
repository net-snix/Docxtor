# Architecture

Read when: touching merge pipeline, helper protocol, macOS app packaging, section handling, relationship rewriting, or validation behavior.

## Flow

1. Human CLI loads args + optional YAML/JSON manifest, or the macOS app writes an `app-run` request JSON file.
2. `Docxtor.Cli` builds a `MergeJob`.
3. `DocxtorMerger` emits merge progress stages.
4. `OpenXmlMergeBackend` runs preflight.
5. Merge executor copies the base document to a temp output path.
6. Each source document is imported in order:
   - optional source-title paragraph from the input filename
   - styles
   - numbering
   - notes/comments
   - relationship-backed content
   - bookmark/drawing ID normalization
   - section/header/footer handling
7. Validators run on the temp output.
8. Temp output moves into place atomically.
9. `merge-report.json` is written separately.
10. In macOS app mode, `Docxtor.Cli app-run` emits NDJSON `started` / `stage` / `completed` / `failed` events to stdout for the SwiftUI shell.

## Main modules

- `Docxtor.Core`
  Backend-neutral models, policies, reports, `DocxtorMerger`.

- `Docxtor.OpenXml`
  Preflight + merge implementation.

- `Docxtor.Validation`
  Open XML schema validation and custom referential-integrity checks.

- `Docxtor.Reporting`
  JSON report writer and temp-file helpers.

- `Docxtor.Cli`
  Command-line parser, manifest loader, job creation, exit codes, hidden `app-run` helper mode.

- `apps/DocxtorMac`
  Native SwiftUI shell. Builds request JSON, launches bundled helper with `Process`, parses NDJSON progress, and shows merge/report UX.

## Important choices

- Package-aware merge, not text copy.
- Default boundary: new-page section break.
- Base theme wins.
- External links preserved, never fetched.
- Fail fast on unsupported high-risk features.
- Open XML schema validation targets modern Word transitional markup (`Office2013`) to avoid false failures on valid Office 2010+ table/style features.
- Source-title insertion is direct paragraph formatting, not a style dependency, so merged docs do not rely on `Heading1` existing in the base file.
- macOS app is a thin shell over the existing .NET engine; merge logic stays shared.

## Known edges

- Visual QA needs render tooling not currently installed in this environment.
- Charts / SmartArt / OLE are intentionally rejected today.
- Tracked-change normalization is not implemented yet.
