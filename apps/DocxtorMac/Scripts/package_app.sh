#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION=${1:-release}
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPO_ROOT="$(cd "${ROOT_DIR}/../.." && pwd)"
APP_NAME=${APP_NAME:-Docxtor}
BUNDLE_ID=${BUNDLE_ID:-com.espenmac.docxtor}
MACOS_MIN_VERSION=${MACOS_MIN_VERSION:-14.0}
SIGNING_MODE=${SIGNING_MODE:-adhoc}
ARCHES_VALUE=${ARCHES:-arm64}
DIST_DIR="${ROOT_DIR}/Dist"
APP_BUNDLE="${DIST_DIR}/${APP_NAME}.app"
HELPER_SOURCE=${DOCXTOR_HELPER_PATH:-}
HELPER_CONFIGURATION=${HELPER_CONFIGURATION:-Release}
HELPER_RUNTIME=${HELPER_RUNTIME:-osx-arm64}
HELPER_PROJECT=${HELPER_PROJECT:-${REPO_ROOT}/src/Docxtor.Cli/Docxtor.Cli.csproj}
HELPER_PUBLISH_DIR="${ROOT_DIR}/.build/helper-publish/${HELPER_RUNTIME}"

if [[ -f "${ROOT_DIR}/version.env" ]]; then
  # shellcheck disable=SC1091
  source "${ROOT_DIR}/version.env"
else
  MARKETING_VERSION=${MARKETING_VERSION:-0.1.0}
  BUILD_NUMBER=${BUILD_NUMBER:-1}
fi

read -r -a ARCH_LIST <<<"${ARCHES_VALUE}"

log() { printf '%s\n' "$*"; }
fail() { printf 'ERROR: %s\n' "$*" >&2; exit 1; }

build_product_path() {
  local name="$1"
  local arch="$2"
  echo "${ROOT_DIR}/.build/${arch}-apple-macosx/${CONFIGURATION}/${name}"
}

publish_helper() {
  [[ -f "${HELPER_PROJECT}" ]] || fail "helper project missing: ${HELPER_PROJECT}"
  mkdir -p "${HELPER_PUBLISH_DIR}"
  log "==> dotnet publish helper (${HELPER_CONFIGURATION}, ${HELPER_RUNTIME})"
  dotnet publish "${HELPER_PROJECT}" \
    -c "${HELPER_CONFIGURATION}" \
    -r "${HELPER_RUNTIME}" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o "${HELPER_PUBLISH_DIR}"
  HELPER_SOURCE="${HELPER_PUBLISH_DIR}/Docxtor.Cli"
}

for arch in "${ARCH_LIST[@]}"; do
  log "==> swift build (${CONFIGURATION}, ${arch})"
  swift build -c "${CONFIGURATION}" --arch "${arch}" --package-path "${ROOT_DIR}"
done

rm -rf "${APP_BUNDLE}"
mkdir -p \
  "${APP_BUNDLE}/Contents/MacOS" \
  "${APP_BUNDLE}/Contents/Resources/DocxtorHelper" \
  "${DIST_DIR}"

if [[ ${#ARCH_LIST[@]} -gt 1 ]]; then
  INPUTS=()
  for arch in "${ARCH_LIST[@]}"; do
    INPUTS+=("$(build_product_path "${APP_NAME}" "${arch}")")
  done
  lipo -create "${INPUTS[@]}" -output "${APP_BUNDLE}/Contents/MacOS/${APP_NAME}"
else
  cp "$(build_product_path "${APP_NAME}" "${ARCH_LIST[0]}")" "${APP_BUNDLE}/Contents/MacOS/${APP_NAME}"
fi

chmod +x "${APP_BUNDLE}/Contents/MacOS/${APP_NAME}"

cat > "${APP_BUNDLE}/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>${APP_NAME}</string>
    <key>CFBundleDisplayName</key><string>${APP_NAME}</string>
    <key>CFBundleIdentifier</key><string>${BUNDLE_ID}</string>
    <key>CFBundleExecutable</key><string>${APP_NAME}</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleShortVersionString</key><string>${MARKETING_VERSION}</string>
    <key>CFBundleVersion</key><string>${BUILD_NUMBER}</string>
    <key>LSMinimumSystemVersion</key><string>${MACOS_MIN_VERSION}</string>
    <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
PLIST

if [[ -n "${HELPER_SOURCE}" ]]; then
  [[ -x "${HELPER_SOURCE}" ]] || fail "DOCXTOR_HELPER_PATH is not executable: ${HELPER_SOURCE}"
else
  publish_helper
fi

if [[ -n "${HELPER_SOURCE}" ]]; then
  cp "${HELPER_SOURCE}" "${APP_BUNDLE}/Contents/Resources/DocxtorHelper/Docxtor.Cli"
  chmod +x "${APP_BUNDLE}/Contents/Resources/DocxtorHelper/Docxtor.Cli"
  log "==> bundled helper ${HELPER_SOURCE}"
fi

xattr -cr "${APP_BUNDLE}"

if [[ "${SIGNING_MODE}" == "adhoc" ]]; then
  codesign --force --sign "-" "${APP_BUNDLE}/Contents/MacOS/${APP_NAME}"
  if [[ -f "${APP_BUNDLE}/Contents/Resources/DocxtorHelper/Docxtor.Cli" ]]; then
    codesign --force --sign "-" "${APP_BUNDLE}/Contents/Resources/DocxtorHelper/Docxtor.Cli"
  fi
  codesign --force --sign "-" "${APP_BUNDLE}"
fi

log "Created ${APP_BUNDLE}"
