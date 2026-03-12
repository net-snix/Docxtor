# DocxtorMac

Native macOS shell for Docxtor.

## What is here

- SwiftPM SwiftUI app
- single-window merge UI
- persisted `Source titles` toggle to insert each filename before its merged segment
- helper bridge for `Docxtor.Cli app-run --request ...`
- local packaging scripts

## Runtime helper contract

The packaged app bundles the helper here:

`Docxtor.app/Contents/Resources/DocxtorHelper/Docxtor.Cli`

Or override it before launch:

```bash
export DOCXTOR_HELPER_PATH=/absolute/path/to/Docxtor.Cli
```

Helper stdout must emit one JSON object per line:

```json
{"type":"started","message":"Merge started"}
{"type":"stage","stage":"preflight","message":"Running preflight"}
{"type":"stage","stage":"merging-input","currentInputIndex":1,"totalInputs":3,"inputDisplayName":"a.docx"}
{"type":"completed","outputPath":"/tmp/main.docx","reportPath":"/tmp/main.merge-report.json"}
```

Helper request JSON includes:

```json
{
  "inputs":["/tmp/a.docx","/tmp/b.docx"],
  "outputPath":"/tmp/main.docx",
  "reportPath":"/tmp/main.merge-report.json",
  "templatePath":null,
  "insertSourceFileTitles":true
}
```

Failure shape:

```json
{"type":"failed","message":"Tracked changes are not supported.","reportPath":"/tmp/main.merge-report.json"}
```

## Local build

```bash
swift test
./Scripts/dev.sh --test
```

Default packaging flow:

- runs `dotnet publish` for `src/Docxtor.Cli`
- builds the SwiftUI app with SwiftPM
- assembles `Dist/Docxtor.app`
- bundles the published helper into `Contents/Resources/DocxtorHelper/Docxtor.Cli`
- ad-hoc signs the helper and app bundle

## Packaging vars

- `DOCXTOR_HELPER_PATH`: optional helper binary override; skips helper publish when set
- `SIGNING_MODE=adhoc`: default local ad-hoc signing
- `ARCHES="arm64"`: default arm64 build
- `HELPER_CONFIGURATION=Release`: helper publish configuration
- `HELPER_RUNTIME=osx-arm64`: helper publish runtime
