# SPEC.MD

## Unified DOCX Merger — Comprehensive Product and Technical Specification

**Document status:** Draft for implementation  
**Version:** 1.0  
**Primary output:** `main.docx`  
**Primary stack target:** .NET 8 + Open XML SDK backend  
**Optional backends:** Python `docxcompose` adapter for MVP, Aspose.Words adapter for commercial high-fidelity shortcut

---

## 1. Purpose

This specification defines the product, behavioral, functional, technical, validation, and release requirements for a program that accepts **one or more `.docx` files** and produces **one merged `.docx` file**—by default named `main.docx`—while preserving:

- images
- text formatting
- paragraph and character styles
- lists and numbering
- tables
- hyperlinks and bookmarks
- section boundaries
- page setup
- headers and footers
- notes and comments where supported by the selected backend
- package integrity and internal relationships

This document is written to be implementation-ready. It complements `PLAN.MD` by turning the architectural direction into a concrete engineering specification.

---

## 2. Problem Statement

Merging Word files is not a text concatenation task. A `.docx` document is an Open Packaging Conventions (OPC) ZIP package containing multiple XML and binary parts connected by relationships. A correct merger must therefore preserve and rewrite package-level dependencies instead of only copying paragraphs.

Naive approaches fail because imported content may depend on:

- relationship-backed images or drawings
- numbering definitions
- styles and style inheritance
- section properties
- headers/footers
- footnotes/endnotes/comments
- charts, embedded objects, and other auxiliary parts

The system must treat merge as a **document package graph transformation**.

---

## 3. Goals

### 3.1 Primary goals

1. Merge **N `.docx` files** into a single valid `.docx`.
2. Preserve visible fidelity as closely as practical.
3. Preserve package consistency and referential integrity.
4. Provide both a **CLI** and a **library API**.
5. Support deterministic and testable behavior.
6. Scale to large numbers of inputs without a hard-coded file count limit.

### 3.2 Secondary goals

1. Allow multiple backends behind a stable interface.
2. Produce machine-readable diagnostics and validation reports.
3. Support progressive release maturity from MVP to high-fidelity engine.
4. Keep implementation cross-platform for Windows, Linux, and macOS.

---

## 4. Non-goals

The following are explicitly out of scope for the core project unless a later release adds them:

- input formats other than `.docx` in the main pipeline (`.doc`, `.pdf`, `.odt`, `.rtf`, `.docm`)
- semantic merging of duplicate content
- natural-language conflict resolution across documents
- WYSIWYG document editing features
- server-hosted collaboration features
- OCR or document understanding
- guaranteed perfect preservation of every vendor-specific or rare OOXML extension on day one

---

## 5. Normative Language

The keywords **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, and **MAY** are used in the normative sense:

- **MUST / MUST NOT**: required for conformance
- **SHOULD / SHOULD NOT**: recommended unless there is a documented reason otherwise
- **MAY**: optional capability

---

## 6. Definitions

- **Source document**: an input `.docx` file being merged.
- **Destination document**: the evolving merged document package.
- **Base document**: the initial destination package; by default the first source, unless an explicit template is provided.
- **Backend**: a merge implementation behind a common interface.
- **Part**: an OPC package part such as `document.xml`, `styles.xml`, or an image part.
- **Relationship**: an OPC link connecting a part to another part or an external resource.
- **Section**: a WordprocessingML page-layout scope controlled by `w:sectPr`.
- **Preflight**: inspection pass that inventories features and determines merge feasibility.
- **Capability matrix**: the feature support profile of a backend.
- **Fidelity**: how closely the merged output matches the visual and structural meaning of the inputs.

---

## 7. Product Overview

The product is a command-line and library-based DOCX merger with the following operating model:

1. Load input list and merge configuration.
2. Inspect all inputs to inventory features and risks.
3. Select the appropriate backend.
4. Merge sources sequentially into a destination package.
5. Validate the resulting document.
6. Emit:
   - `main.docx` (or configured output path)
   - `merge-report.json`
   - logs / diagnostics

Default user expectation:

- inputs are merged in the exact order supplied
- the output is one file
- images and formatting survive
- section/page settings are preserved where supported
- the merge fails loudly rather than silently dropping important content

---

## 8. Users and Use Cases

### 8.1 Primary users

- developers embedding document merge into applications
- engineers building document-processing pipelines
- office/publishing workflows that assemble reports from chapters
- legal, academic, or business users combining structured documents

### 8.2 Core use cases

1. Merge chapter files into one report.
2. Merge templated cover + body + appendix into one deliverable.
3. Merge many small generated documents into one archive document.
4. Build a pipeline-safe merger that emits validation reports.
5. Choose a fast backend for simple documents and a high-fidelity backend for complex documents.

---

## 9. Scope by Release

To keep the project realistic and shippable, requirements are grouped by release.

### 9.1 Release A — MVP

Purpose: a working merger for ordinary business documents.

Included:

- CLI
- ordered input merge
- one output `.docx`
- basic text, runs, paragraphs, tables
- embedded images
- standard styles and numbering
- docxcompose backend
- warning/reporting for flattened or unsupported complex features

Limitations:

- non-base headers/footers may be flattened
- advanced section fidelity is limited
- tracked changes normalization not required
- charts / SmartArt / OLE not required

### 9.2 Release B — Production Open-Source Engine

Purpose: robust open-source, high-fidelity merge core.

Included:

- custom Open XML SDK merge backend
- section preservation
- header/footer preservation
- style conflict handling
- numbering remapping
- notes/comments remapping
- referential integrity validation
- machine-readable reports
- visual regression tests

### 9.3 Release C — Advanced Fidelity

Purpose: harden edge cases and enterprise-grade behavior.

Included:

- chart/diagram/OLE hardening
- theme conflict policy options
- tracked-changes normalization modes
- richer field handling
- optional Aspose adapter
- advanced performance tuning and deterministic packaging modes

---

## 10. System Context and Architecture Summary

The system SHALL be structured as:

```text
CLI / Library API
    |
    v
Configuration Loader / Manifest Parser
    |
    v
Preflight Inspector
    |
    +--> Feature Inventory
    +--> Capability Matching
    +--> Safety / Corruption Checks
    |
    v
Merge Planner
    |
    +--> Base Document Selection
    +--> Boundary Strategy
    +--> Backend Selection
    +--> ID Allocation Strategy
    |
    v
Merge Backend
    |
    +--> Body Clone / Import
    +--> Relationship Copier
    +--> Style Merger
    +--> Numbering Merger
    +--> Notes/Comments Merger
    +--> Section/Header/Footer Merger
    +--> ID Normalizer
    +--> Package Updater
    |
    v
Validator
    |
    +--> Open XML Validation
    +--> Referential Integrity Checks
    +--> Optional Render-based QA
    |
    v
Output Writer
    |
    +--> main.docx
    +--> merge-report.json
```

---

## 11. Product Defaults

Unless overridden by configuration or CLI flags, the system SHALL use the following defaults:

- **base document policy**: first input document
- **output filename**: `main.docx`
- **ordering policy**: preserve exact input order
- **boundary policy**: insert a new-page section break between source documents
- **section policy**: preserve source sections where backend supports it
- **header/footer policy**: preserve source section headers/footers
- **numbering policy**: preserve source numbering
- **image deduplication**: enabled
- **tracked changes policy**: fail preflight unless an explicit normalization mode is chosen
- **altChunk policy**: reject or resolve before merge; never treat unresolved `altChunk` as acceptable final output
- **theme policy**: base theme wins, with warning on theme conflicts
- **external-resource policy**: preserve external links, but do not auto-fetch network resources

---

## 12. Functional Requirements

### 12.1 Input and Job Handling

**FR-001** The program MUST accept **1..N** `.docx` input files.  
**FR-002** The program MUST NOT impose a hard-coded upper limit on the number of input files. Practical limits MAY depend on available resources.  
**FR-003** The program MUST preserve the exact input order unless an explicit order override is supplied.  
**FR-004** The program MUST support input via:
- positional CLI arguments
- config/manifest file
- library API list of paths/streams

**FR-005** Duplicate file paths in the input list MUST be treated as distinct occurrences if they appear multiple times in order.  
**FR-006** Non-`.docx` inputs MUST be rejected unless a future conversion stage is explicitly added.  
**FR-007** Password-protected, encrypted, or corrupted `.docx` files MUST fail preflight with a clear error.  
**FR-008** The system MUST support both filesystem paths and stream-based library input where possible.

### 12.2 Output Handling

**FR-009** The program MUST emit one merged `.docx`.  
**FR-010** The default output path MUST be `main.docx` in the current working directory unless overridden.  
**FR-011** The output write MUST be atomic from the user’s point of view: write to a temporary file and finalize only after successful validation/save.  
**FR-012** The system MUST produce a machine-readable report file named `merge-report.json` unless explicitly disabled.  
**FR-013** On failure, the system MUST NOT leave a partially-written final output file at the target path.

### 12.3 Preflight and Capability Detection

**FR-014** Before merge, the system MUST inspect each input and create a feature inventory.  
**FR-015** The feature inventory MUST detect at least:
- headers
- footers
- footnotes
- endnotes
- comments
- tracked changes
- charts
- SmartArt/diagram parts
- embedded objects / OLE
- text boxes / drawings
- external hyperlinks
- external images
- bookmarks
- numbering
- styles
- `stylesWithEffects`
- themes
- `altChunk`

**FR-016** Preflight MUST determine whether the selected backend can safely process the job.  
**FR-017** If the requested backend cannot meet required feature preservation, the system MUST:
1. switch to a more capable backend when allowed, or
2. fail with a capability error.  
Silent feature dropping is not allowed for normative features.

### 12.4 Body Content Preservation

**FR-018** The system MUST preserve paragraph order and body content order.  
**FR-019** The system MUST preserve run-level formatting, including bold, italic, underline, font properties, language, highlighting, and character style references.  
**FR-020** The system MUST preserve paragraph-level formatting, including alignment, indentation, spacing, tabs, borders, shading, and paragraph style references.  
**FR-021** The system MUST preserve tables, including row/cell structure, merges, widths, borders, shading, and table styles.  
**FR-022** The system MUST preserve page breaks and explicit line/page break markup present in source content.  
**FR-023** The system SHOULD preserve fields, content controls, and references unless a backend-specific limitation applies; any limitation MUST be reported.

### 12.5 Images and Drawings

**FR-024** Embedded images MUST be preserved.  
**FR-025** Image dimensions and positioning metadata MUST be preserved.  
**FR-026** Alternate text, captions, and non-visual properties SHOULD be preserved when present.  
**FR-027** Image formats MUST NOT be converted unless an explicit normalization option is provided.  
**FR-028** Reused identical images SHOULD be deduplicated by binary hash in the destination package when safe.  
**FR-029** Drawing relationships MUST be rewritten to valid destination relationships.  
**FR-030** Floating images and text boxes MUST be preserved in the advanced backend; MVP MAY warn and best-effort only.

### 12.6 Styles and Formatting Semantics

**FR-031** The merger MUST analyze style usage for imported content.  
**FR-032** If an imported style ID does not exist in destination, the style MUST be copied.  
**FR-033** If an imported style ID exists and is structurally equivalent, the style SHOULD be reused.  
**FR-034** If an imported style ID exists but differs semantically, the imported style MUST be renamed and all references updated.  
**FR-035** Transitive style references (`basedOn`, `next`, linked styles, numbering style references, etc.) MUST be updated when style IDs are renamed.  
**FR-036** `stylesWithEffects` SHOULD be merged when present to improve modern Word round-tripping.  
**FR-037** The system MUST NOT silently remap conflicting styles to unrelated definitions.

### 12.7 Lists and Numbering

**FR-038** The system MUST preserve bullets and numbering.  
**FR-039** Imported numbering definitions MUST be copied or reused as needed.  
**FR-040** Numbering IDs (`numId`, `abstractNumId`) MUST be remapped if collisions occur.  
**FR-041** The default policy MUST be `PreserveSourceNumbering`.  
**FR-042** The system MAY expose an alternative `ContinueDestinationNumbering` policy.  
**FR-043** Any numbering policy applied MUST be declared in the merge report.

### 12.8 Hyperlinks, Bookmarks, and Internal References

**FR-044** External hyperlinks MUST be preserved as external relationships.  
**FR-045** Internal anchors/bookmark links MUST be preserved.  
**FR-046** Bookmark IDs MUST be remapped to avoid collisions.  
**FR-047** If bookmark names collide, the system MUST deterministically rename imported bookmarks and update references that target those names.  
**FR-048** The merger MUST NOT fetch external resources from the network unless an explicit opt-in feature is added.

### 12.9 Sections, Page Setup, Headers, and Footers

**FR-049** Section layout MUST be treated as first-class merge data.  
**FR-050** The default merge boundary between source documents MUST be a new-page section break.  
**FR-051** Existing internal sections inside each source MUST be preserved in the production backend.  
**FR-052** Page setup properties such as margins, orientation, paper size, and page-number settings SHOULD be preserved per section.  
**FR-053** Headers and footers referenced by imported sections MUST be copied with valid relationships.  
**FR-054** The engine MUST respect WordprocessingML section semantics:
- non-final sections store `sectPr` on the last paragraph of the section
- the final section stores `sectPr` at body level

**FR-055** The merge MUST leave the destination with a valid final body-level `sectPr`.  
**FR-056** Even/odd and first-page header/footer settings MUST be preserved when supported by the backend.  
**FR-057** If a selected backend cannot preserve per-source section headers/footers, it MUST report that behavior explicitly before merge and in the output report.

### 12.10 Footnotes, Endnotes, Comments

**FR-058** The production backend MUST preserve footnotes, endnotes, and comments.  
**FR-059** Referenced notes/comments MUST be copied with remapped IDs.  
**FR-060** Unreferenced notes/comments SHOULD NOT be imported unless a diagnostic or preservation mode explicitly requests them.  
**FR-061** Reserved separator and continuation note entries in note parts MUST be handled according to OOXML semantics and MUST NOT be duplicated blindly.  
**FR-062** Relationships used inside imported notes/comments MUST also be copied or rewritten.

### 12.11 Charts, Diagrams, Embedded Objects

**FR-063** Release A MAY warn and fail on unsupported chart/diagram/OLE content.  
**FR-064** Release B SHOULD preflight-detect chart/diagram/OLE content and either preserve it or fail clearly.  
**FR-065** Release C SHOULD preserve chart-related part graphs, embedded workbook parts, and OLE relationships when present.  
**FR-066** No backend may silently discard known chart/diagram/OLE parts without warning.

### 12.12 Tracked Changes and Review Markup

**FR-067** The default policy for tracked changes MUST be `fail`.  
**FR-068** The system MAY add normalization modes such as `accept-all` or `reject-all`.  
**FR-069** If tracked changes are present and the current policy is unsupported by the backend, preflight MUST fail.  
**FR-070** Review markup behavior MUST be recorded in the report.

### 12.13 altChunk

**FR-071** Unresolved `altChunk` content MUST be rejected by default.  
**FR-072** A future resolver MAY normalize `altChunk` into ordinary WordprocessingML before merge.  
**FR-073** The final merged document MUST NOT rely on unresolved `altChunk` as its primary representation.

### 12.14 Reporting and Diagnostics

**FR-074** The system MUST produce a structured merge report.  
**FR-075** The report MUST include:
- input list
- chosen backend
- detected features
- merge policies
- warnings
- remap counts
- validation results
- duration metrics
- failure details if unsuccessful

**FR-076** Logs SHOULD be available in both human-readable and machine-readable forms.  
**FR-077** The CLI MUST return non-zero exit codes on failure.

---

## 13. Non-Functional Requirements

### 13.1 Fidelity

**NFR-001** The merged document MUST remain structurally valid as a `.docx` package.  
**NFR-002** The system SHOULD maximize visible fidelity relative to what Microsoft Word would display for the sources.  
**NFR-003** The system MUST prefer correctness over silent degradation.

### 13.2 Determinism

**NFR-004** Given the same inputs, options, backend version, and runtime environment, the system SHOULD produce semantically identical output.  
**NFR-005** The order of imported content and ID remapping MUST be deterministic.  
**NFR-006** The report content MUST be deterministic except for timestamps, durations, and explicitly non-deterministic metadata.

### 13.3 Performance and Scalability

**NFR-007** The merge MUST process inputs sequentially rather than loading all sources fully into memory when avoidable.  
**NFR-008** The program SHOULD keep at most one active source package plus one destination package open at a time.  
**NFR-009** The system MUST support large input counts without algorithmic dependence on a fixed number of files.  
**NFR-010** The implementation SHOULD expose profiling metrics for:
- input count
- total input bytes
- output bytes
- image count
- merge time
- validation time

### 13.4 Resource Management

**NFR-011** Source packages MUST be disposed promptly after import.  
**NFR-012** Temporary files MUST be cleaned up on success and SHOULD be cleaned up on failure.  
**NFR-013** Optional checkpointing MAY be implemented for very large jobs.

### 13.5 Maintainability

**NFR-014** The system MUST isolate backend-specific logic behind stable interfaces.  
**NFR-015** Core policy and report models MUST be backend-neutral.  
**NFR-016** Each merge subsystem SHOULD be unit-testable independently.

### 13.6 Security

**NFR-017** The merger MUST NOT execute macros or embedded code.  
**NFR-018** The merger MUST NOT fetch remote content unless explicitly enabled by the user.  
**NFR-019** The merger SHOULD treat external relationships as untrusted data.  
**NFR-020** The merger MUST fail safely on malformed package structures.

### 13.7 Cross-Platform

**NFR-021** The CLI and primary library MUST run on Windows, Linux, and macOS.  
**NFR-022** Validation features that depend on optional external renderers MAY vary by environment, but the core merge must remain cross-platform.

---

## 14. Detailed Merge Semantics

This section defines how the merger SHALL interpret and transform source documents.

### 14.1 Base Document Policy

1. If `template` is specified, that package becomes the base.
2. Otherwise, the first input becomes the base.
3. Imported content from subsequent sources is appended according to the selected boundary policy.
4. Base-level global parts remain authoritative unless the policy explicitly allows replacement.

Default global precedence:

- theme: base wins
- settings: base wins with selective merges for required flags
- document properties: base wins unless merge policy says otherwise

### 14.2 Boundary Policy

Supported boundary modes:

- `section-new-page` (default)
- `page-break`
- `continuous-section`
- `none` (expert mode only)

Normative behavior:

- default MUST be `section-new-page`
- if `preserve source sections` is enabled, the backend MUST ensure section semantics remain valid
- `page-break` MUST NOT be chosen implicitly when section preservation is required

### 14.3 Body Import Semantics

For each imported source:

1. Resolve or reject policy-sensitive features (`tracked changes`, `altChunk`).
2. Clone body children in document order.
3. Exclude the source’s trailing final body-level `sectPr` from naïve insertion.
4. Materialize section boundaries according to WordprocessingML rules.
5. Append imported content after the chosen boundary marker.
6. Restore a valid final body-level `sectPr` after all sources are processed.

### 14.4 Relationship Copy Semantics

When imported markup references an internal or external relationship:

1. determine the source part that owns the relationship
2. create an equivalent destination relationship
3. copy the target part graph if internal
4. preserve external targets without dereferencing them
5. rewrite relationship IDs in imported XML

Supported relationship-backed content includes:

- images
- hyperlinks
- charts
- diagram parts
- embedded packages / OLE
- headers/footers
- notes/comments assets
- custom XML references where applicable

### 14.5 Style Merge Semantics

Style handling SHALL follow this policy:

1. discover directly and transitively used styles in imported content
2. compare candidate imported style definitions against destination definitions
3. if equivalent, reuse destination style ID
4. if absent, copy style
5. if same ID but different definition, rename imported style ID
6. rewrite all references to renamed styles
7. update linked style references and inheritance chains

Equivalence SHOULD be based on normalized structural comparison, not raw XML string equality.

### 14.6 Numbering Merge Semantics

Numbering handling SHALL follow this policy:

1. discover all `numId` references in imported content
2. locate referenced `w:num` and `w:abstractNum` definitions
3. reuse equivalent destination definitions where safe
4. otherwise allocate fresh destination IDs
5. rewrite imported numbering references
6. preserve source list appearance by default

### 14.7 Section/Header/Footer Merge Semantics

Section handling SHALL follow this policy:

1. preserve internal sections of the imported document when the backend supports it
2. copy header/footer parts referenced by imported section properties
3. allocate fresh relationship IDs in the destination main document part
4. rewrite section header/footer references to the new IDs
5. preserve page setup attributes when possible
6. ensure final document has exactly one valid body-level `sectPr`

Global settings interaction:

- if any imported section relies on even/odd headers, destination settings MUST enable it
- if title page behavior is needed, imported section properties MUST preserve that setting

### 14.8 Notes and Comments Semantics

Notes/comments handling SHALL follow this policy:

1. collect only referenced footnotes/endnotes/comments from imported content
2. allocate new destination IDs
3. copy referenced note/comment bodies
4. copy relationship-backed content used inside those bodies
5. rewrite note/comment references in imported content
6. preserve author and metadata when available
7. do not duplicate special separator entries

### 14.9 Bookmark and Anchor Semantics

1. bookmark IDs MUST be unique in final output
2. bookmark names SHOULD be preserved when unique
3. on name collision, imported names MUST be deterministically renamed
4. internal references that target renamed bookmark names MUST be updated

### 14.10 Theme Semantics

Because Word themes are effectively document-wide, the default theme policy SHALL be:

- base document theme wins
- imported themed content uses the resulting final theme context
- theme conflicts generate warnings in the report

Optional future policies MAY include:

- replace-with-template-theme
- import-first-source-theme
- explicit harmonization strategy

### 14.11 Document Properties Semantics

By default:

- core document properties: base wins
- custom properties: merge by name if identical, otherwise base wins and conflict is reported
- application properties: base wins

Future policies MAY expose finer control.

### 14.12 Fields and TOC Semantics

The merger SHOULD preserve field codes and field markup.  
The merger MAY set the “update fields on open” signal so Word refreshes derived fields such as TOC or page counts.  
The merger MUST report whether field-refresh signaling was applied.

---

## 15. Backend Abstraction

The project SHALL support more than one merge backend through a common interface.

### 15.1 Required backend contract

Each backend MUST expose:

- name and version
- capability matrix
- preflight inspection support
- merge execution
- structured diagnostics

### 15.2 Suggested interface

```csharp
public interface IMergeBackend
{
    string Name { get; }
    string Version { get; }
    BackendCapabilities GetCapabilities();

    Task<PreflightResult> InspectAsync(
        IReadOnlyList<InputDocument> inputs,
        MergePolicy policy,
        CancellationToken cancellationToken = default);

    Task<MergeResult> MergeAsync(
        MergeJob job,
        CancellationToken cancellationToken = default);
}
```

### 15.3 Required backends

#### Backend 1: `openxml-sdk`

Status: primary production backend

Responsibilities:

- full package-aware merge
- style/numbering remap
- section/header/footer handling
- notes/comments handling
- validation integration

#### Backend 2: `docxcompose`

Status: optional MVP backend

Responsibilities:

- fast path for ordinary documents
- reduced fidelity on known complex cases
- explicit capability disclosure

#### Backend 3: `aspose` (optional)

Status: optional commercial adapter

Responsibilities:

- enterprise shortcut for high-fidelity imports
- imported as a separate package/module to avoid mandatory commercial dependency

### 15.4 Backend selection policy

1. If user explicitly selects a backend, use it if capabilities satisfy the job.
2. If not, select the least-cost backend that satisfies detected features and requested policies.
3. If no backend can satisfy the job, fail preflight with actionable diagnostics.

---

## 16. Data Model

### 16.1 Core models

```text
MergeJob
  - Inputs: List<InputDocument>
  - OutputPath: string
  - Policy: MergePolicy
  - Validation: ValidationPolicy
  - BackendHint: string?
  - TemplatePath: string?
  - WorkingDirectory: string?
  - CorrelationId: string

InputDocument
  - PathOrId: string
  - DisplayName: string
  - SizeBytes: long?
  - SourceIndex: int

MergePolicy
  - BoundaryMode
  - PreserveSourceSections
  - PreserveHeadersFooters
  - NumberingMode
  - ThemePolicy
  - TrackedChangesMode
  - AltChunkMode
  - ExternalResourceMode
  - ImageDeduplication
  - DeterministicIds
  - UpdateFieldsOnOpen

ValidationPolicy
  - RunOpenXmlValidation
  - RunReferentialIntegrityChecks
  - RunVisualRegression
  - EmitReport
  - FailOnWarnings
```

### 16.2 Feature inventory model

```text
FeatureInventory
  - HasHeaders
  - HasFooters
  - HasFootnotes
  - HasEndnotes
  - HasComments
  - HasTrackedChanges
  - HasCharts
  - HasSmartArt
  - HasEmbeddedObjects
  - HasTextBoxes
  - HasExternalHyperlinks
  - HasExternalImages
  - HasBookmarks
  - HasNumbering
  - HasStyles
  - HasStylesWithEffects
  - HasTheme
  - HasAltChunk
  - HasFields
  - HasContentControls
  - PartCounts
  - RelationshipCounts
```

### 16.3 ID remapping model

```text
IdMap
  - RelationshipIdMap
  - StyleIdMap
  - NumIdMap
  - AbstractNumIdMap
  - FootnoteIdMap
  - EndnoteIdMap
  - CommentIdMap
  - BookmarkIdMap
  - BookmarkNameMap
  - DrawingIdMap
  - NonVisualPictureIdMap
```

### 16.4 Report model

```text
MergeReport
  - CorrelationId
  - Status
  - StartedAtUtc
  - FinishedAtUtc
  - DurationMs
  - OutputPath
  - OutputSizeBytes
  - Backend
  - Policy
  - InputSummaries[]
  - PreflightWarnings[]
  - MergeWarnings[]
  - RemapCounts
  - Validation
  - Error
```

---

## 17. CLI Specification

### 17.1 Command shape

```bash
docx-merge [options] input1.docx input2.docx ... inputN.docx
```

### 17.2 Required options

- none; positional inputs are sufficient for basic usage

### 17.3 Supported options

```text
--output <path>                       Output DOCX path (default: main.docx)
--config <path>                       YAML/JSON config file
--template <path>                     Explicit base/template DOCX
--backend <name>                      openxml-sdk | docxcompose | aspose
--boundary <mode>                     section-new-page | page-break | continuous-section | none
--preserve-sections                   true|false
--preserve-headers-footers            true|false
--numbering <mode>                    preserve-source | continue-destination
--tracked-changes <mode>              fail | accept-all | reject-all
--altchunk <mode>                     reject | resolve
--theme-policy <mode>                 base-wins | import-first | template-wins
--external-resources <mode>           preserve-links | materialize
--image-dedup                         true|false
--update-fields-on-open               true|false
--validate-openxml                    true|false
--validate-references                 true|false
--visual-qa                           true|false
--report <path>                       Path to merge-report.json
--log-format <mode>                   text | json
--fail-on-warnings                    true|false
--dry-run                             Preflight only; do not merge
--version
--help
```

### 17.4 Exit codes

Suggested exit codes:

- `0` success
- `1` generic failure
- `2` invalid arguments
- `3` preflight capability failure
- `4` validation failure
- `5` output write failure
- `6` corrupted or encrypted input
- `7` backend unavailable

### 17.5 CLI examples

Basic merge:

```bash
docx-merge --output main.docx cover.docx chapter1.docx chapter2.docx appendix.docx
```

Use explicit template and production backend:

```bash
docx-merge \
  --template template.docx \
  --backend openxml-sdk \
  --boundary section-new-page \
  --preserve-sections true \
  --preserve-headers-footers true \
  --numbering preserve-source \
  --output main.docx \
  intro.docx body.docx appendix.docx
```

Preflight only:

```bash
docx-merge --dry-run --report merge-report.json *.docx
```

---

## 18. Configuration File Specification

The merger SHOULD support YAML and JSON configuration files.

### 18.1 Example YAML

```yaml
inputs:
  - cover.docx
  - chapters/01-intro.docx
  - chapters/02-method.docx
  - chapters/03-results.docx
  - appendix.docx

template: null
output: main.docx
backend: openxml-sdk

merge:
  boundary: section-new-page
  preserve_sections: true
  preserve_headers_footers: true
  numbering: preserve-source
  tracked_changes: fail
  altchunk: reject
  theme_policy: base-wins
  external_resources: preserve-links
  image_dedup: true
  update_fields_on_open: true

validation:
  openxml: true
  references: true
  visual_qa: false
  emit_report: true
  fail_on_warnings: false

report:
  path: merge-report.json
  log_format: text
```

### 18.2 Precedence rules

1. CLI options override config file values.
2. Config file values override built-in defaults.
3. Environment variables MAY be added later but SHALL be lowest precedence unless documented otherwise.

---

## 19. Library API Specification

The library API SHOULD expose a simple high-level entry point and lower-level building blocks.

### 19.1 High-level API

```csharp
public interface IDocxMerger
{
    Task<MergeResult> MergeAsync(MergeJob job, CancellationToken cancellationToken = default);
    Task<PreflightResult> PreflightAsync(MergeJob job, CancellationToken cancellationToken = default);
}
```

### 19.2 Result types

```csharp
public sealed class MergeResult
{
    public bool Success { get; init; }
    public string OutputPath { get; init; } = "";
    public MergeReport Report { get; init; } = new();
}
```

### 19.3 Stream support

The API SHOULD support:

- file path input/output
- stream input/output
- in-memory merge in controlled scenarios

The API MUST document any backend restrictions on stream usage.

---

## 20. Validation Specification

### 20.1 Validation stages

The system SHALL validate in this order:

1. package save success
2. Open XML schema validation
3. custom referential integrity checks
4. optional visual QA

### 20.2 Referential integrity checks

The custom validator MUST verify at least:

- all relationship targets exist for internal relationships
- all imported `r:id` values resolve within their owning part
- all referenced styles exist
- all referenced numbering definitions exist
- all footnote/endnote/comment references resolve
- all required content types are present
- unique-ID constraints after normalization
- valid final body-level `sectPr`

### 20.3 Visual QA

Visual QA SHOULD render merged documents to PDF or page images and compare against expected fixtures for regression tests.  
Visual QA is primarily a test-suite feature, not a mandatory runtime production step.

### 20.4 Validation outcomes

Validation MUST be recorded as:

- `Passed`
- `PassedWithWarnings`
- `Failed`

Failure MUST include actionable diagnostics.

---

## 21. Error Handling and Reporting

### 21.1 Error policy

The system SHALL prefer **fail-fast with explanation** over silent corruption.

### 21.2 Error categories

- invalid arguments
- unreadable input
- encrypted input
- corrupted package
- unsupported feature for selected backend
- merge algorithm failure
- validation failure
- output write failure

### 21.3 Error message requirements

Errors MUST identify:

- which input document triggered the issue when applicable
- which feature or part caused failure
- which backend was in use
- whether a more capable backend might resolve the issue

### 21.4 Warning policy

Warnings SHOULD be emitted for:

- theme conflicts
- header/footer flattening
- fallback backend selection
- unsupported optional features preserved on a best-effort basis
- skipped visual QA
- duplicate bookmark name renaming
- field-refresh ambiguity

---

## 22. Observability

The system SHOULD emit structured telemetry fields such as:

- correlation ID
- backend name/version
- input count
- total input size
- part counts
- relationship counts
- style remap counts
- numbering remap counts
- image dedup counts
- warning counts
- elapsed durations per phase

This data SHOULD appear in both logs and `merge-report.json`.

---

## 23. Suggested Repository Layout

```text
/src
  /DocxMerge.Cli
  /DocxMerge.Core
  /DocxMerge.OpenXmlBackend
  /DocxMerge.DocxComposeAdapter
  /DocxMerge.AsposeAdapter
  /DocxMerge.Validation
  /DocxMerge.Reporting

/tests
  /DocxMerge.UnitTests
  /DocxMerge.IntegrationTests
  /DocxMerge.VisualRegressionTests

/fixtures
  /basic
  /images
  /tables
  /styles
  /numbering
  /sections
  /headers-footers
  /notes-comments
  /charts
  /ole
  /theme-conflicts

/docs
  PLAN.MD
  SPEC.MD
```

---

## 24. Internal Component Specification

### 24.1 Preflight Inspector

Responsibilities:

- open source documents read-only
- detect features
- detect corruption and unsupported package structure
- calculate complexity and backend requirements
- produce warnings and capability decisions

Outputs:

- `FeatureInventory` per input
- `PreflightWarning[]`
- `BackendRecommendation`
- `PreflightResult`

### 24.2 Merge Planner

Responsibilities:

- choose base document
- decide backend
- decide boundary strategy
- allocate ID generation/remap strategies
- produce merge plan that downstream components can execute deterministically

### 24.3 Body Cloner

Responsibilities:

- clone body elements in order
- preserve run/paragraph/table markup
- exclude or relocate `sectPr` as required
- insert merge boundaries

### 24.4 Relationship Copier

Responsibilities:

- discover relationship-backed references inside imported XML
- copy internal target parts recursively
- recreate external relationships
- rewrite `r:id` attributes
- deduplicate identical binary targets where safe

### 24.5 Style Merger

Responsibilities:

- collect used styles
- compare and merge style definitions
- rename conflicting styles
- rewrite style references
- merge linked style structures

### 24.6 Numbering Merger

Responsibilities:

- collect numbering references
- reuse or copy numbering definitions
- allocate destination IDs
- rewrite list references

### 24.7 Notes / Comments Merger

Responsibilities:

- collect referenced notes/comments only
- allocate IDs
- copy bodies and dependent relationships
- rewrite references

### 24.8 Section / Header / Footer Merger

Responsibilities:

- preserve section layout
- copy related header/footer parts
- rewrite references
- maintain valid final section model

### 24.9 ID Normalizer

Responsibilities:

- ensure uniqueness for:
  - relationship IDs per part
  - bookmark IDs
  - bookmark names where required
  - comment IDs
  - footnote/endnote IDs
  - drawing IDs
  - non-visual picture IDs
  - numbering IDs

### 24.10 Package Updater

Responsibilities:

- update `[Content_Types].xml`
- add missing overrides/defaults
- maintain package properties
- emit update-fields-on-open setting when configured

### 24.11 Validator

Responsibilities:

- run Open XML validation
- run referential integrity checks
- summarize validation in report
- surface fatal and non-fatal issues

---

## 25. Algorithm Specification

### 25.1 Preflight algorithm

1. Parse config and inputs.
2. Validate paths and extensions.
3. Open each `.docx` read-only.
4. Build feature inventory.
5. Detect corruption/encryption.
6. Compare required features against backend capabilities.
7. Produce `PreflightResult`.

### 25.2 Merge algorithm

1. Choose base document.
2. Copy base to working destination.
3. For each remaining source:
   1. apply pre-merge normalization rules
   2. insert boundary
   3. clone body content
   4. copy/rewrite relationships
   5. merge styles
   6. merge numbering
   7. merge notes/comments
   8. merge sections and header/footer parts
   9. normalize IDs
4. finalize final body-level `sectPr`
5. update content types and package metadata
6. save to temp output
7. validate
8. finalize output
9. emit report

### 25.3 Failure behavior

If any stage fails:

- stop processing
- record failure phase
- emit partial diagnostics/report when possible
- do not publish incomplete final output

---

## 26. Acceptance Criteria

### 26.1 Core acceptance criteria

A build is acceptable for Release B when all of the following are true:

1. Merging 10 ordinary `.docx` files with text, tables, lists, and images produces a valid output with no missing images.
2. Style collisions are resolved without visual corruption in the provided fixture suite.
3. Numbering remains correct for multilevel list fixtures.
4. Mixed portrait/landscape sections remain correct in merged output.
5. Different section-specific headers/footers remain correct in merged output.
6. Footnotes, endnotes, and comments survive with valid references.
7. Open XML validation passes for the merged fixture corpus.
8. Referential integrity checks pass.
9. `merge-report.json` is produced and populated.
10. No silent dropping of detected normative features occurs.

### 26.2 Release A acceptance criteria

Release A is acceptable when:

- standard text formatting survives
- tables survive
- embedded images survive
- lists survive
- CLI works
- report works
- known header/footer flattening is explicitly reported

### 26.3 Release C acceptance criteria

Release C is acceptable when:

- chart and OLE fixtures either pass with preservation or fail with precise diagnostics
- theme conflict policies are implemented and tested
- tracked changes normalization modes are tested and documented
- visual regression suite covers advanced fixtures

---

## 27. Test Strategy

### 27.1 Test layers

1. **Unit tests**
   - style equivalence
   - ID remapping
   - numbering remap
   - bookmark renaming
   - report generation
   - backend capability matching

2. **Integration tests**
   - end-to-end merge of representative documents
   - validation outcomes
   - CLI exit codes

3. **Visual regression tests**
   - render merged outputs to PDF/images
   - compare page-level results

4. **Stress tests**
   - many small inputs
   - large image-heavy inputs
   - mixed feature corpora

### 27.2 Fixture matrix

The fixture suite SHOULD include:

- plain text paragraphs
- character formatting variations
- paragraph formatting variations
- tables with merged cells
- inline images
- floating images
- repeated image dedup cases
- simple bullets
- multilevel numbering
- conflicting style IDs with same names
- conflicting style IDs with different definitions
- section changes with different margins/orientation
- first-page headers
- even/odd headers
- footnotes
- endnotes
- comments
- bookmarks/internal anchors
- hyperlinks
- fields / TOC
- charts
- embedded workbook
- SmartArt / diagrams
- OLE objects
- theme conflicts
- tracked changes
- `altChunk`

### 27.3 Golden-output policy

Where byte-for-byte equality is impractical, tests SHOULD compare:

- validation pass/fail
- expected page count
- expected image count
- expected relationship count ranges
- visual output
- semantic XML invariants

---

## 28. Performance and Scaling Targets

These are engineering targets, not protocol constraints.

### 28.1 Baseline targets

On reference developer hardware, the production backend SHOULD handle:

- at least 100 ordinary `.docx` inputs in one job
- sequential processing without a hard-coded file-count limit
- image-heavy documents without duplicating identical binaries unnecessarily

### 28.2 Memory strategy

The engine SHOULD:

- process one source at a time
- avoid full corpus expansion in memory
- dispose source packages promptly
- checkpoint optionally for long-running merges

### 28.3 Measurement

Performance tests SHOULD capture:

- total runtime
- peak working set if measurable
- per-phase durations
- output size growth
- dedup savings

---

## 29. Security and Robustness Considerations

1. Treat all input documents as untrusted.
2. Do not execute scripts/macros or embedded code.
3. Do not dereference remote URLs by default.
4. Guard against ZIP bombs or pathological package structures where feasible.
5. Validate relationship targets before dereferencing internal parts.
6. Reject malformed XML/OPC structures with explicit diagnostics.
7. Preserve external links as links; do not silently embed fetched content.

---

## 30. Open Questions and Future Enhancements

These are not blockers for initial implementation but should remain visible:

1. Should deterministic ZIP entry ordering and timestamps be added for reproducible builds?
2. Should theme harmonization be made automatic or policy-driven only?
3. Should field updates be delegated to Word on open or refreshed by external tooling?
4. How aggressively should equivalent numbering/style definitions be deduplicated?
5. Should a “best-effort” mode ever be allowed for advanced objects, or should all unsupported features remain hard failures?
6. Should `.docm` be supported later while stripping or preserving macros safely?
7. Should a server mode or REST API be added later?

---

## 31. Implementation Roadmap

### 31.1 Milestone 1 — skeleton

- repository setup
- core models
- CLI parser
- report schema
- backend interface
- preflight scaffolding

### 31.2 Milestone 2 — MVP backend

- docxcompose adapter
- basic merge job path
- warnings and capability matrix
- basic fixtures and report output

### 31.3 Milestone 3 — production backend core

- Open XML SDK package loader
- body import
- relationship copying
- styles
- numbering
- IDs
- validation

### 31.4 Milestone 4 — section fidelity

- section handling
- header/footer merge
- mixed layout fixtures
- page setup preservation

### 31.5 Milestone 5 — notes/comments and hardening

- footnotes/endnotes/comments
- bookmarks/internal references
- richer diagnostics
- stress tests

### 31.6 Milestone 6 — advanced fidelity

- charts / OLE / SmartArt
- theme options
- tracked changes normalization
- optional Aspose adapter

---

## 32. Traceability Matrix

| Requirement Area | Primary Components | Verification |
|---|---|---|
| Input handling | CLI, Config Loader, Preflight | Unit + integration |
| Style preservation | Style Merger | Unit + integration + visual |
| Numbering preservation | Numbering Merger | Unit + integration + visual |
| Image preservation | Relationship Copier | Integration + visual |
| Section fidelity | Section/Header/Footer Merger | Integration + visual |
| Notes/comments | Notes/Comments Merger | Integration |
| Validation | Validator | Integration |
| Reporting | Reporting module | Unit + integration |
| Backend selection | Preflight + Planner | Unit |

---

## 33. Recommended Initial Decisions

The project SHOULD begin with the following concrete choices:

1. Use **.NET 8** for the main implementation.
2. Use **Open XML SDK** as the primary long-term backend.
3. Keep a backend interface from day one.
4. Add a **docxcompose** adapter only as an optional MVP shortcut, not as the final fidelity engine.
5. Make **fail-fast with diagnostics** the default behavior for unsupported complex features.
6. Treat **section/header/footer correctness** as a core requirement, not a later patch.
7. Build **validation and reporting** into the main pipeline rather than as an afterthought.

---

## 34. References

This specification is derived from the research and recommendations captured in `PLAN.MD`. The implementation and design assumptions in that document rely on the following reference set:

- Microsoft Open XML SDK package and project documentation
- Microsoft Learn documentation for WordprocessingML document structure
- ECMA / OOXML relationships documentation
- Microsoft documentation for `SectionProperties` (`w:sectPr`)
- `docxcompose` package and source code
- Aspose.Words append/import documentation
- docx4j documentation on merge complexity and `altChunk`
- Microsoft documentation for theme parts and `stylesWithEffects`

For the exact source URLs and citation set, see `PLAN.MD`.

---

## 35. Final Conformance Statement

A conforming implementation of this specification SHALL:

1. accept one or more `.docx` inputs,
2. produce one valid merged `.docx`,
3. preserve images and formatting for supported features,
4. preserve package integrity and internal references,
5. report unsupported or degraded cases explicitly,
6. provide deterministic, testable behavior through a CLI and library API.

