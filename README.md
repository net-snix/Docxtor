# Docxtor

High-fidelity `.docx` merger. CLI + library API + native macOS app shell. Open XML SDK backend. Deterministic merge/report flow.

## Status

- Working backend: `openxml-sdk`
- Output: merged `.docx` + `merge-report.json`
- Native macOS app: `apps/DocxtorMac`
- Optional source titles: insert each file name before its merged segment
- Preflight: feature inventory + capability checks
- Validation: Open XML schema + referential integrity
- Visual QA: wired, skipped unless render tooling is installed

## Dev target

- Spec target: `.NET 8`
- Local runnable target in this repo: `net10.0`

Reason: this machine has a `.NET 10` runtime installed and no local `.NET 8` runtime for execution.

## Usage

```bash
dotnet run --project /Users/espenmac/Code/Docxtor/src/Docxtor.Cli -- \
  --output main.docx \
  --report merge-report.json \
  intro.docx chapter1.docx appendix.docx
```

Dry run:

```bash
dotnet run --project /Users/espenmac/Code/Docxtor/src/Docxtor.Cli -- \
  --dry-run \
  --report merge-report.json \
  intro.docx chapter1.docx
```

Config file:

```bash
dotnet run --project /Users/espenmac/Code/Docxtor/src/Docxtor.Cli -- \
  --config merge.yaml
```

Config manifests larger than 1 MiB are rejected (fail-fast guard against accidental or malicious oversized inputs).

macOS app:

```bash
cd /Users/espenmac/Code/Docxtor/apps/DocxtorMac
./Scripts/dev.sh --test
```

That flow publishes a self-contained `Docxtor.Cli` helper for `osx-arm64`, packages `Docxtor.app`, bundles the helper inside the app, then launches it.

## Supported now

- ordered merge for `1..N` `.docx`
- base doc = first input or explicit template
- paragraphs, runs, tables
- embedded images and hyperlinks
- style merge with collision rename
- numbering remap
- bookmarks + drawing ID normalization
- section preservation
- header/footer part copy
- footnotes, endnotes, comments
- atomic output write
- machine-readable report

## Fail-fast limits

Current backend rejects during preflight:

- tracked changes when policy is `fail`
- unresolved `altChunk`
- charts
- SmartArt / diagram parts
- embedded packages / OLE
- external-resource materialization
- continue-destination numbering
- output/report/template path collisions that could overwrite inputs or each other

## Layout

```text
src/
  Docxtor.Cli
  Docxtor.Core
  Docxtor.OpenXml
  Docxtor.Reporting
  Docxtor.Validation

tests/
  Docxtor.UnitTests
  Docxtor.IntegrationTests

docs/
  SPEC.MD
  PLAN.MD
  ARCHITECTURE.md

apps/
  DocxtorMac
```

## Build

```bash
dotnet build /Users/espenmac/Code/Docxtor/Docxtor.slnx
dotnet test /Users/espenmac/Code/Docxtor/Docxtor.slnx
```
