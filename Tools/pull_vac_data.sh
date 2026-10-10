#!/usr/bin/env bash
set -euo pipefail

PACKAGE_ID="${VAC_PACKAGE_ID:-com.DefaultCompany.VAC}"
REMOTE="/sdcard/Android/data/${PACKAGE_ID}/files/VACExperimentData"
OUT="${1:-VACExperimentData_pull_$(date +%Y%m%d_%H%M%S)}"

if command -v adb >/dev/null 2>&1; then
  ADB="$(command -v adb)"
else
  UNITY_ADB="/Applications/Unity/Hub/Editor/6000.2.15f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
  if [[ ! -x "$UNITY_ADB" ]]; then
    echo "adb not found. Set PATH or edit UNITY_ADB in this script." >&2
    exit 1
  fi
  ADB="$UNITY_ADB"
fi

echo "Using adb: $ADB"
"$ADB" get-state >/dev/null
mkdir -p "$OUT"
echo "Pulling: $REMOTE"
"$ADB" pull "$REMOTE" "$OUT/"
echo
echo "Done: $OUT/VACExperimentData"
