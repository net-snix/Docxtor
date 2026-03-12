#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APP_NAME=${APP_NAME:-Docxtor}
APP_BUNDLE="${ROOT_DIR}/Dist/${APP_NAME}.app"
RUN_TESTS=0
CONFIGURATION=${CONFIGURATION:-debug}

log() { printf '%s\n' "$*"; }
fail() { printf 'ERROR: %s\n' "$*" >&2; exit 1; }

for arg in "$@"; do
  case "${arg}" in
    --test|-t) RUN_TESTS=1 ;;
    --release) CONFIGURATION=release ;;
    --help|-h)
      log "Usage: $(basename "$0") [--test] [--release]"
      exit 0
      ;;
  esac
done

log "==> stop existing ${APP_NAME}"
pkill -f "${APP_NAME}.app/Contents/MacOS/${APP_NAME}" 2>/dev/null || true
pkill -x "${APP_NAME}" 2>/dev/null || true

if [[ "${RUN_TESTS}" == "1" ]]; then
  log "==> swift test"
  swift test --package-path "${ROOT_DIR}"
fi

log "==> package app"
"${ROOT_DIR}/Scripts/package_app.sh" "${CONFIGURATION}"

[[ -d "${APP_BUNDLE}" ]] || fail "missing app bundle at ${APP_BUNDLE}"

log "==> launch app"
open "${APP_BUNDLE}"
