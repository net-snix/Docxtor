# PLAN.MD

## Title
High-fidelity `.docx` merger architecture for building `main.docx` from N input files

## Goal
Create a program that accepts any practical number of `.docx` files and produces one merged `main.docx` while preserving images, text formatting, lists/numbering, tables, section layout, and other package-backed Word content as faithfully as possible.

## Executive recommendation
Build the production version as a **.NET 8 CLI/service using the Microsoft Open XML SDK (`DocumentFormat.OpenXml` 3.4.1)**, and treat the merge as an **OOXML package/part merge**, not as paragraph copying. This is the best open-source foundation because `.docx` is a ZIP/OPC package made of interrelated parts and relationships, and the Open XML SDK gives direct access to those structures. Microsoft’s own SDK docs note that it supports combining files, but also that it is low-level and assumes detailed format knowledge.[R1][R2]

For a faster MVP, keep a **Python backend based on `docxcompose` 2.0.2** as an optional implementation for ordinary business documents. It is current again in 2026 and its codebase explicitly handles images, footnotes, styles, and numbering when appending documents. However, its published docs also state that the **first document’s headers/footers are used throughout the merged file**, and its public PR list still includes open work for **endnotes**, so I would not make it the only backend if the target is “preserve everything.”[R6][R7][R8]

If you are comfortable with a commercial dependency, the quickest path to a high-fidelity merger is **Aspose.Words**. Its append/import APIs expose style, numbering, header/footer, and textbox-related import options directly.[R9][R10]

## What the research says

### 1. Why naïve approaches fail
- A `.docx` file is not just text; it is a package of XML parts and binary parts tied together by relationships.[R3][R4]
- Important content can live outside the main body: headers, footers, comments, footnotes, endnotes, styles, numbering, settings, theme, charts, embedded packages, and more.[R3]
- A merge that only copies paragraph text, or only splices `word/document.xml`, will break as soon as the imported content depends on related parts such as images, notes, numbering, or section-specific headers/footers.
- `w:altChunk` exists, but it is not a robust final architecture for a self-contained merger. Docx4j’s own documentation explains that without its MergeDocx extension you must rely on Word to convert `altChunk` into normal content, which means a round-trip through Word if you need a normalized document afterwards.[R11]

### 2. Current viable implementation paths

| Option | Freshness | Strengths | Limits | Recommendation |
|---|---|---|---|---|
| Open XML SDK 3.4.1 (.NET) | Current in 2026 | Full access to parts, rels, validation, standards-oriented, official Microsoft SDK | More engineering work; SDK is intentionally low-level | **Best open-source production foundation** |
| docxcompose 2.0.2 (Python) | Current in 2026 | Simple append API; current package; code handles images/footnotes/styles/numbering | Later docs’ headers/footers are ignored; endnote coverage is not something I would assume complete | **Best MVP/open-source shortcut** |
| Aspose.Words | Current in 2026 | High-level append/import API; import options for numbering, headers/footers, text boxes | Commercial license | **Best paid shortcut** |
| docx4j MergeDocx | Current commercial option | Mature Java ecosystem; designed for merge use case | Commercial enterprise add-on | Good if your stack is Java |

### 3. Important OOXML facts that shape the design
- WordprocessingML content is organized around a main document body, but many other parts can be present in a typical document.[R3]
- Relationships are the mechanism that connect XML markup to images, charts, hyperlinks, embedded objects, and other internal or external targets.[R4]
- Section properties are tricky: for **all sections except the last**, `w:sectPr` lives under the **last paragraph of that section**; for the **final section**, `w:sectPr` lives directly under `w:body`.[R5]
- Styles live in dedicated style parts, and newer Word files may also carry a `stylesWithEffects` part that should be considered during merge decisions.[R13]
- The document theme is a dedicated part attached to the main document part and controls overall look/feel for themed content. This means theme behavior is effectively document-wide, not per-section.[R12]

## Scope for v1
### In scope
- Input: one or more `.docx` files.
- Output: one `main.docx`.
- Preserve body content, tables, text formatting, lists/numbering, images, hyperlinks, bookmarks, section boundaries, headers/footers, footnotes, endnotes, and comments where supported.
- Handle multiple source documents in deterministic order.
- Provide both CLI and library API.
- Perform structural validation and visual QA after merge.

### Out of scope for v1
- `.doc`, `.pdf`, `.odt`, and `.docm` input.
- Semantic deduplication of duplicated text across documents.
- Perfect preservation of every obscure vendor extension on day one.
- Rich edit operations after merge beyond minimal cleanup.

## Product defaults
- **Base document policy:** the first input document is the template/base unless an explicit template is supplied.
- **Ordering policy:** preserve input order exactly.
- **Boundary policy:** insert a **new-page section break** between source documents by default.
- **Header/footer policy:** preserve source section headers/footers by default.
- **Numbering policy:** preserve source numbering by default.
- **Tracked changes policy:** fail preflight by default unless a normalization policy is explicitly chosen.
- **`altChunk` policy:** reject or resolve before merge; do not carry `altChunk` through as a primary merged representation.

## Recommended architecture

```text
CLI / API
   |
   v
Job Loader ----> Config / Manifest
   |
   v
Preflight Inspector
   |
   +--> Feature inventory (styles, numbering, notes, charts, headers/footers, tracked changes, altChunk)
   +--> Safety checks (encrypted/corrupt/unsupported docs)
   |
   v
Merge Planner
   |
   +--> Base document selection
   +--> Boundary plan (page break vs section break)
   +--> Per-doc merge policy
   |
   v
OOXML Merge Core
   |
   +--> Body Cloner
   +--> Relationship Copier
   +--> Style Merger
   +--> Numbering Merger
   +--> Notes/Comments Merger
   +--> Section/HeaderFooter Merger
   +--> ID Normalizer
   +--> ContentTypes / Package Updater
   |
   v
Validator
   |
   +--> Open XML validation
   +--> Custom referential-integrity validation
   |
   v
QA Renderer
   |
   +--> render DOCX -> PDF/PNG
   +--> page count / visual diff / fixture comparison
   |
   v
main.docx
```

## Main components

### 1. CLI / public API
Expose both:
- a CLI for local usage and pipelines
- a library API for embedding into other tools or services

Example CLI shape:

```bash
docx-merge   --output main.docx   --boundary section:new-page   --preserve-headers-footers   --keep-source-numbering   intro.docx chapter1.docx chapter2.docx appendix.docx
```

### 2. Preflight Inspector
Responsibilities:
- open each document read-only
- inventory features and complexity:
  - headers/footers
  - footnotes/endnotes/comments
  - tracked changes
  - charts / diagrams / OLE
  - text boxes / drawings
  - external hyperlinks
  - `altChunk`
  - theme / styles / numbering presence
- reject encrypted or unsupported inputs
- flag documents that require the advanced merge path

Output:
- `FeatureInventory` for each source
- `MergeWarnings`
- `MergeCapabilityDecision`

### 3. Merge Planner
Responsibilities:
- choose the base/template doc
- determine whether each boundary is a page break or section break
- choose merge mode:
  - `UniformHeadersFooters`
  - `PreserveSourceSections`
- allocate ID remapping tables before content copy starts

### 4. OOXML Merge Core
This is the heart of the system.

#### 4.1 Body Cloner
- clone all source body children except the source’s final `w:sectPr`
- insert the cloned content into destination body at the correct boundary
- when preserving sections, materialize section boundaries correctly instead of flattening them

#### 4.2 Relationship Copier
- scan cloned markup for relationship-backed content
- recreate internal or external relationships in destination parts
- recursively copy dependent parts when needed
- rewrite `r:id` values to new destination relationship IDs

Relationship-backed content to support:
- images
- hyperlinks
- charts
- SmartArt / diagram parts
- embedded packages / OLE
- header/footer parts
- note/comment related parts if present
- external image links

Image handling strategy:
- deduplicate by binary hash so repeated logos/images are not copied many times
- preserve original image format and dimensions

#### 4.3 Style Merger
- gather used style IDs from imported content
- if a style does not exist in destination: copy it
- if a style ID exists and the definition is equivalent: reuse it
- if a style ID exists and definitions differ:
  - rename imported style ID
  - update style references in imported content
  - update transitive style links (`basedOn`, `next`, `link`, etc.)
- include `stylesWithEffects` when present so newer Word documents round-trip better.[R13]

#### 4.4 Numbering Merger
- detect imported `w:numId` references
- copy referenced `w:num` and `w:abstractNum` definitions
- allocate fresh destination IDs
- rewrite all imported numbering references
- allow a policy switch:
  - `PreserveSourceNumbering` (default)
  - `ContinueDestinationNumbering`

#### 4.5 Notes / comments merger
Support these independently:
- footnotes
- endnotes
- comments

Algorithm:
- allocate new IDs in destination parts
- copy only the referenced notes/comments
- recursively copy any relationships used inside those notes/comments
- rewrite references in imported body content

#### 4.6 Section / header / footer merger
This is where many merge attempts fail.

Rules:
- preserve each source section’s page setup where possible
- copy header/footer parts and rewrite section references to the new rel IDs
- respect the WordprocessingML rule that the final section uses body-level `sectPr`, while earlier sections use paragraph-level `sectPr`.[R5]

Modes:
- `PreserveSourceSections` (recommended)
- `UnifyWithBaseHeadersFooters` (simpler mode, docxcompose-like)

#### 4.7 ID normalizer
Normalize or reallocate IDs that must be unique in the final package, including:
- bookmark IDs
- comment IDs
- footnote/endnote IDs
- drawing IDs (`wp:docPr`)
- non-visual picture IDs (`pic:cNvPr`)
- numbering IDs
- relationship IDs per part

#### 4.8 Package updater
- update `[Content_Types].xml`
- ensure all newly added parts have the right overrides/content types
- preserve or update package properties
- optionally set “update fields on open” if TOC/page fields should refresh in Word

## Data model

```text
MergeJob
  - Inputs[]
  - OutputPath
  - MergePolicy
  - ValidationPolicy

FeatureInventory
  - HasHeadersFooters
  - HasFootnotes
  - HasEndnotes
  - HasComments
  - HasTrackedChanges
  - HasCharts
  - HasSmartArt
  - HasEmbeddedObjects
  - HasAltChunk
  - HasTheme
  - HasStylesWithEffects

IdMap
  - RelIdMap
  - StyleIdMap
  - NumIdMap
  - AbstractNumIdMap
  - FootnoteIdMap
  - EndnoteIdMap
  - CommentIdMap
  - BookmarkIdMap
  - DrawingIdMap
```

## Detailed merge algorithm

### Phase 0: Load and inspect
1. Read all input paths.
2. Verify extension and readability.
3. Build `FeatureInventory` for each file.
4. Reject unsupported files early.

### Phase 1: Select base document
1. Use explicit template if provided.
2. Otherwise use the first input document as the initial destination package.
3. Open all later sources one at a time.

### Phase 2: Normalize policy-sensitive features
Before appending each source:
- if `tracked_changes == fail`, reject when revisions are present
- if `tracked_changes == accept`, normalize first
- if `altChunk` is present, resolve or reject first

### Phase 3: Append a source document
For each source document:
1. Insert boundary marker (default: section break new page).
2. Clone source body content except trailing final `sectPr`.
3. Copy or rewrite all dependency-backed references.
4. Merge styles.
5. Merge numbering.
6. Merge notes/comments.
7. Merge section/header/footer definitions.
8. Normalize IDs.

### Phase 4: Finalize package
1. Restore a valid final body-level `sectPr`.
2. Update content types and relationships.
3. Save to temp output.
4. Run Open XML validation.
5. Run custom referential-integrity checks.
6. Write final `main.docx`.

## Important design decisions

### Do not use `python-docx` alone as the merge engine
`python-docx` is useful for reading/creating/updating Word files, but it is not the right abstraction for a high-fidelity merger by itself. Its docs present it as a document construction/update library, not a package-graph-aware merge engine.[R14]

### Do not use text-only copy
Text-only copy loses images, list identity, note/comment references, and any object that depends on a related part.

### Do not use `altChunk` as the primary final form
`altChunk` is acceptable only as an intermediate import trick if you are willing to round-trip through Word. That is explicitly the behavior docx4j warns about for normalized output.[R11]

### Treat section preservation as first-class
If you ignore section semantics, later documents will often lose header/footer behavior, page orientation, margins, first-page header logic, or page numbering continuity.[R5]

## Scaling and performance
The user requirement says “as many as you’d like.” In practice, this means **streaming the workflow conceptually**, even though ZIP/OPC libraries are not perfect at true streaming.

Recommendations:
- process one source document at a time
- dispose each source package immediately after append
- keep only one active source and one destination package in memory
- optionally checkpoint to temp output after every N documents
- log blob sizes and image counts

This matters especially on .NET because the Open XML SDK project notes a known issue on modern .NET runtimes: ZIP packages do not offer ideal streaming behavior and the working set can grow unexpectedly for large documents.[R2]

## Validation strategy

### Structural validation
- Open XML SDK validation pass
- verify all relationship targets exist
- verify all referenced styles exist
- verify all numbering definitions exist
- verify all note/comment references resolve
- verify unique IDs after normalization

### Visual validation
After every meaningful merge test case:
1. render merged `.docx` to PDF or page images
2. compare with expected fixture renderings
3. inspect page count and obvious layout regressions

Suggested fixture corpus:
- inline images
- floating images
- tables with merged cells
- bullets and multilevel numbering
- different style IDs with identical names
- conflicting style IDs with different definitions
- different headers/footers across sections
- portrait + landscape section mix
- footnotes/endnotes/comments
- charts and embedded workbook
- text boxes and shapes
- theme differences

## Error handling policy
- **Fail fast** on corruption, encryption, or unsupported feature modes.
- **Warn clearly** when falling back from `PreserveSourceSections` to `UnifyWithBaseHeadersFooters`.
- Produce a machine-readable report (`merge-report.json`) with:
  - files merged
  - features detected
  - remap counts
  - warnings
  - validation result

## Suggested release plan

### Release A: MVP
- CLI
- input ordering
- output `main.docx`
- docxcompose backend
- support ordinary text/tables/images/lists
- explicit warning that non-base headers/footers are flattened

### Release B: Production open-source engine
- custom Open XML SDK merge core
- section/header/footer preservation
- notes/comments support
- validation report
- visual regression fixtures

### Release C: Advanced fidelity
- charts / SmartArt / OLE hardening
- theme conflict policies
- tracked-changes normalization modes
- optional commercial backend adapter (Aspose)

## Concrete recommendation
If I were building this now, I would do the following:
1. **Define a backend interface** (`IMergeBackend`) so the CLI is not tied to one library.
2. **Ship an MVP backend with docxcompose** for straightforward docs.[R6][R7]
3. **Implement the long-term backend in .NET + Open XML SDK** for the high-fidelity path.[R1][R2]
4. **Keep Aspose as a drop-in enterprise backend** if you later decide license cost is cheaper than engineering effort.[R9][R10]

This gives you a practical path to a working merger without locking the whole design to the limitations of any one library.

## Minimal config example

```yaml
inputs:
  - cover.docx
  - sections/01-intro.docx
  - sections/02-method.docx
  - sections/03-results.docx
output: main.docx
merge:
  backend: openxml-sdk
  boundary: section-new-page
  preserve_headers_footers: true
  keep_source_numbering: true
  tracked_changes: fail
  altchunk: reject
validation:
  openxml_validate: true
  render_and_compare: true
```

## References
- [R1] NuGet: `DocumentFormat.OpenXml` 3.4.1 — https://www.nuget.org/packages/documentformat.openxml
- [R2] Microsoft Open XML SDK GitHub README — https://github.com/dotnet/Open-XML-SDK
- [R3] Microsoft Learn: Structure of a WordprocessingML document — https://learn.microsoft.com/en-us/office/open-xml/word/structure-of-a-wordprocessingml-document
- [R4] ECMA/OOXML relationships overview — https://c-rex.net/samples/ooxml/e1/Part1/OOXML_P1_Fundamentals_Relationships_topic_ID0EZPBG.html
- [R5] Microsoft Learn: `SectionProperties` / `w:sectPr` behavior — https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.wordprocessing.sectionproperties?view=openxml-3.0.1
- [R6] PyPI: `docxcompose` 2.0.2 — https://pypi.org/project/docxcompose/
- [R7] `docxcompose` source (`composer.py`) showing image/footnote/style/numbering handling — https://github.com/4teamwork/docxcompose/blob/master/docxcompose/composer.py
- [R8] `docxcompose` PR list showing open work such as endnote support — https://github.com/4teamwork/docxcompose/pulls
- [R9] Aspose.Words append/insert docs — https://reference.aspose.com/words/python-net/aspose.words/document/append_document/
- [R10] Aspose import options (`IgnoreHeaderFooter`, `KeepSourceNumbering`, etc.) — https://reference.aspose.com/words/net/aspose.words/importformatoptions/
- [R11] docx4j Getting Started PDF: merge complexity and `altChunk` round-trip note — https://docx4java.org/docx4j/Docx4j_GettingStarted.pdf
- [R12] Microsoft Learn: replace the theme part in a word processing document — https://learn.microsoft.com/en-us/office/open-xml/general/how-to-replace-the-theme-part-in-a-word-processing-document
- [R13] Microsoft Open Specifications: `stylesWithEffects` part — https://learn.microsoft.com/en-us/openspecs/office_standards/ms-docx/6d73a0c5-2d94-461e-81c8-843ad0e911a6
- [R14] `python-docx` docs / PyPI — https://python-docx.readthedocs.io/ and https://pypi.org/project/python-docx/
